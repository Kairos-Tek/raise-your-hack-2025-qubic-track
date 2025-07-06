import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, from } from 'rxjs';
import { catchError, delay, map, switchMap } from 'rxjs/operators';

// Importaciones de la librería Qubic TypeScript
import { QubicTransaction } from '@qubic-lib/qubic-ts-library/dist/qubic-types/QubicTransaction';
import { PublicKey } from '@qubic-lib/qubic-ts-library/dist//qubic-types/PublicKey';
import { Long } from '@qubic-lib/qubic-ts-library/dist//qubic-types/Long';
import { QubicDefinitions } from '@qubic-lib/qubic-ts-library/dist//QubicDefinitions';
import { DynamicPayload } from '@qubic-lib/qubic-ts-library/dist//qubic-types/DynamicPayload';
import { QubicPackageBuilder } from '@qubic-lib/qubic-ts-library/dist//QubicPackageBuilder';

import { TestExecutionResult } from '../shared/models/test-execution-result.model';
import { TestExecutionConfig } from '../shared/models/test-execution-config.model';
import { ContractMethod } from '../shared/models/contract-method.models';
import { SecurityTestCase } from '../shared/models/security-test-case.model';
import { ContractField } from '../shared/models/contract-field.model';
import { ContractAnalysis } from '../shared/models/contract-analysis.model';
import { BroadcastResponse } from '../shared/models/broadcast-response.model';
import { BroadcastRequest } from '../shared/models/broadcast-request.model';
import { TickInfo } from '../shared/models/tick-info.model';
import { TestInputs } from '../shared/models/test-inputs.model';
import { TargetInput } from '../shared/models/target-input';
import { IQubicBuildPackage } from '@qubic-lib/qubic-ts-library/dist/qubic-types/IQubicBuildPackage';
import { TickInfoResponse } from '../shared/models/tick-info.response.model';

type ExecutionStatus = 'success' | 'failed' | 'rejected' | 'error';

@Injectable({
    providedIn: 'root',
})
export class SecurityTestExecutorService {
    constructor(private http: HttpClient) {}

    /**
     * Ejecuta todos los casos de prueba usando los valores reales de Groq
     */
    executeAllSecurityTests(contractAnalysis: ContractAnalysis, config: TestExecutionConfig): Observable<TestExecutionResult[]> {
        console.log(`🧪 Starting security test execution for ${contractAnalysis.contractName}`);

        // Extraer todos los test cases con sus métodos
        const allTests: Array<{ testCase: SecurityTestCase; method: ContractMethod }> = [];

        contractAnalysis.methods.forEach((method: ContractMethod) => {
            if (method.securityTestCases && method.securityTestCases.length > 0) {
                method.securityTestCases.forEach((testCase: SecurityTestCase) => {
                    allTests.push({ testCase, method });
                });
            }
        });

        console.log(`📊 Found ${allTests.length} test cases to execute`);

        // Ejecutar tests con delay entre cada uno
        const testObservables = allTests.map((testData, index) =>
            this.executeSecurityTest(testData.testCase, testData.method, config).pipe(
                delay(index * config.delayBetweenTests),
                catchError((error: Error) => {
                    console.error(`Error in test ${testData.testCase.testName}:`, error);
                    return from([this.createErrorResult(testData.testCase, testData.method, error.message)]);
                }),
            ),
        );

        return forkJoin(testObservables);
    }

    /**
     * Ejecuta un caso de prueba específico
     */
    public executeSecurityTest(testCase: SecurityTestCase, method: ContractMethod, config: TestExecutionConfig): Observable<TestExecutionResult> {
        console.log(`🔬 Executing: ${testCase.testName}`);

        const testInputs = this.parseTestInputs(testCase.testInputs);
        console.log(`🎯 Target: ${testCase.targetVariable} with value: ${testInputs?.targetInput?.maliciousValue}`);

        return this.getCurrentTick(config.qubicApiUrl).pipe(
            switchMap((currentTick: number) => {
                try {
                    // 1. Preparar inputs con valores específicos de Groq
                    const transactionInputs = this.prepareInputsFromTestCase(testCase, method, testInputs);

                    // 2. Crear payload dinámico
                    const payload = this.createDynamicPayload(method, transactionInputs);

                    // 3. Crear transacción
                    const targetTick = currentTick + config.tickOffset;

                    return this.createQubicTransaction(config.testIdentity, config.testSeed, config.contractAddress, method, payload, targetTick).pipe(
                        switchMap((transaction: QubicTransaction) => {
                            return this.broadcastTransaction(transaction, config.qubicApiUrl).pipe(
                                map((result: BroadcastResponse) => this.createSuccessResult(testCase, method, transaction, payload, result)),
                                catchError((error: Error) => {
                                    console.error(`Broadcast failed for ${testCase.testName}:`, error);
                                    return from([this.createErrorResult(testCase, method, `Broadcast failed: ${error.message}`)]);
                                }),
                            );
                        }),
                    );
                } catch (error: unknown) {
                    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
                    console.error(`Error preparing test ${testCase.testName}:`, error);
                    return from([this.createErrorResult(testCase, method, errorMessage)]);
                }
            }),
            catchError((error: Error) => {
                console.error(`Error getting tick for ${testCase.testName}:`, error);
                return from([this.createErrorResult(testCase, method, `Failed to get current tick: ${error.message}`)]);
            }),
        );
    }

    /**
     * Parsea los testInputs del SecurityTestCase
     */
    private parseTestInputs(testInputs: Record<string, any>): TestInputs | null {
        if (!testInputs || typeof testInputs !== 'object') {
            return null;
        }

        const targetInput = testInputs['targetInput'] as TargetInput;
        const otherInputs = testInputs['otherInputs'] as Record<string, string>;

        if (!targetInput || !targetInput.variableName || !targetInput.maliciousValue) {
            return null;
        }

        return {
            targetInput,
            otherInputs: otherInputs || {},
        };
    }

    /**
     * Prepara inputs usando los valores específicos del test case de Groq
     */
    private prepareInputsFromTestCase(
        testCase: SecurityTestCase,
        method: ContractMethod,
        testInputs: TestInputs | null,
    ): Record<string, PublicKey | Long | boolean | string | any[]> {
        if (!testInputs || !testInputs.targetInput) {
            throw new Error(`Test case ${testCase.testName} missing testInputs with targetInput`);
        }

        const inputs: Record<string, PublicKey | Long | boolean | string | any[]> = {};

        // 1. Agregar valores de otherInputs
        if (testInputs.otherInputs) {
            Object.entries(testInputs.otherInputs).forEach(([fieldName, value]: [string, string]) => {
                const field = method.inputFields.find((f: ContractField) => f.name === fieldName);
                if (field) {
                    try {
                        inputs[fieldName] = this.convertToQubicTypeRobust(value, field);
                        console.log(`📝 Other input ${fieldName}: ${value} -> converted successfully`);
                    } catch (error) {
                        console.error(`Error converting other input ${fieldName}:`, error);
                        inputs[fieldName] = this.getDefaultValue(field);
                    }
                }
            });
        }

        // 2. Sobrescribir con el valor malicioso del targetInput
        const targetField = method.inputFields.find((f: ContractField) => f.name === testInputs.targetInput.variableName);
        if (targetField) {
            const maliciousValue = testInputs.targetInput.maliciousValue;
            try {
                inputs[testInputs.targetInput.variableName] = this.convertToQubicTypeRobust(maliciousValue, targetField);
                console.log(`🎯 Malicious input ${targetField.name}: ${maliciousValue} -> converted successfully`);
            } catch (error) {
                console.error(`Error converting malicious value for ${targetField.name}:`, error);
                // Para valores maliciosos, intentar de todas formas para que el test pueda proceder
                inputs[testInputs.targetInput.variableName] = this.getDefaultValue(targetField);
            }
        }

        // 3. Rellenar campos faltantes con valores por defecto
        method.inputFields.forEach((field: ContractField) => {
            if (!(field.name in inputs)) {
                inputs[field.name] = this.getDefaultValue(field);
                console.log(`🔧 Default ${field.name}: set to default value`);
            }
        });

        // 4. Validar todos los valores finales
        for (const [fieldName, value] of Object.entries(inputs)) {
            const field = method.inputFields.find((f: ContractField) => f.name === fieldName);
            if (field && !this.validateQubicValue(value, field)) {
                console.warn(`Invalid value for ${fieldName}: expected ${field.typeScriptType}, got ${typeof value}`);
            }
        }

        return inputs;
    }

    /**
     * Convierte string a tipo Qubic apropiado
     */
    private convertToQubicTypeRobust(value: string, field: ContractField): PublicKey | Long | boolean | string | any[] {
        // Si value es undefined o null, usar valor por defecto
        if (value === undefined || value === null) {
            return this.getDefaultValue(field);
        }

        // Convertir a string si no lo es
        const stringValue = String(value);

        try {
            switch (field.typeScriptType) {
                case 'PublicKey':
                    // Validar longitud de public key
                    if (stringValue.length !== 60) {
                        console.warn(`Invalid PublicKey length: ${stringValue.length}, expected 60. Using default.`);
                        return this.getDefaultValue(field);
                    }
                    return new PublicKey(stringValue);

                case 'Long':
                    // Validar que sea numérico
                    if (!/^-?\d+$/.test(stringValue)) {
                        console.warn(`Invalid Long value: ${stringValue}. Using default.`);
                        return this.getDefaultValue(field);
                    }

                    try {
                        // Para números muy grandes, usar BigInt directamente
                        const bigIntValue = BigInt(stringValue);
                        return new Long(bigIntValue);
                    } catch (error) {
                        console.warn(`Error creating Long from ${stringValue}:`, error);
                        return this.getDefaultValue(field);
                    }

                case 'boolean':
                    const lowerValue = stringValue.toLowerCase();
                    return lowerValue === 'true' || lowerValue === '1' || lowerValue === 'yes';

                case 'string':
                    return stringValue;

                case 'Array<any>':
                    try {
                        const parsed = JSON.parse(stringValue);
                        return Array.isArray(parsed) ? parsed : [parsed];
                    } catch {
                        return [stringValue];
                    }

                default:
                    console.warn(`Unknown TypeScript type: ${field.typeScriptType}`);
                    return stringValue;
            }
        } catch (error) {
            console.error(`Error converting "${stringValue}" to ${field.typeScriptType}:`, error);
            return this.getDefaultValue(field);
        }
    }

    /**
     * Obtiene valor por defecto para un campo
     */
    private getDefaultValue(field: ContractField): PublicKey | Long | boolean | string | any[] {
        switch (field.typeScriptType) {
            case 'PublicKey':
                // Usar una public key válida por defecto
                return new PublicKey('CFBMEMZOIDEXQAUXYYSZIURADQLAPWPMNJXQSNVQZAHYVOPYUKKJBJUCTVJL');

            case 'Long':
                // Valor por defecto según el tipo Qubic usando BigInt
                if (field.qubicType && field.qubicType.includes('uint')) {
                    return new Long(BigInt(1000)); // Valor positivo por defecto
                } else {
                    return new Long(BigInt(0)); // Cero por defecto para signed
                }

            case 'boolean':
                return false;

            case 'string':
                return 'default';

            case 'Array<any>':
                return [];

            default:
                return 'unknown';
        }
    }

    /**
     * Método de validación para valores de entrada
     */
    private validateQubicValue(value: unknown, field: ContractField): boolean {
        switch (field.typeScriptType) {
            case 'PublicKey':
                return value instanceof PublicKey;

            case 'Long':
                return value instanceof Long;

            case 'boolean':
                return typeof value === 'boolean';

            case 'string':
                return typeof value === 'string';

            case 'Array<any>':
                return Array.isArray(value);

            default:
                return true; // Aceptar cualquier valor para tipos desconocidos
        }
    }

    /**
     * Crea payload dinámico
     */
    private createDynamicPayload(method: ContractMethod, inputs: Record<string, PublicKey | Long | boolean | string | any[]>): DynamicPayload {
        console.log(`📦 Creating payload for ${method.name} (${method.packageSize} bytes)`);

        const builder = new QubicPackageBuilder(method.packageSize);

        // Agregar campos en orden
        const sortedFields = [...method.inputFields].sort((a: ContractField, b: ContractField) => a.order - b.order);

        sortedFields.forEach((field: ContractField) => {
            const value = inputs[field.name];
            if (value !== undefined) {
                console.log(`  📋 Adding ${field.name}: ${field.typeScriptType} (${field.byteSize} bytes)`);

                // Usar el método específico según el tipo de valor
                if (value instanceof PublicKey) {
                    builder.add(value); // PublicKey implementa IQubicBuildPackage
                } else if (value instanceof Long) {
                    builder.add(value); // Long implementa IQubicBuildPackage
                } else if (typeof value === 'boolean') {
                    // Para boolean, convertir a uint8 (0 o 1)
                    builder.addInt(value ? 1 : 0);
                } else if (typeof value === 'string') {
                    // Para string, convertir a Uint8Array
                    const encoder = new TextEncoder();
                    const stringBytes = encoder.encode(value);
                    builder.adduint8Array(stringBytes);
                } else if (typeof value === 'number') {
                    // Para números, usar addInt o addShort según el tamaño
                    if (field.byteSize <= 2) {
                        builder.addShort(value);
                    } else {
                        builder.addInt(value);
                    }
                } else if (Array.isArray(value)) {
                    // Para arrays, serializar como JSON y convertir a bytes
                    const jsonString = JSON.stringify(value);
                    const encoder = new TextEncoder();
                    const arrayBytes = encoder.encode(jsonString);
                    builder.adduint8Array(arrayBytes);
                } else {
                    // Para otros tipos, intentar usar add() si implementa IQubicBuildPackage
                    try {
                        builder.add(value as IQubicBuildPackage);
                    } catch (error) {
                        console.warn(`Unable to add ${field.name} with value ${value}, using raw bytes`);
                        // Como último recurso, convertir a string y luego a bytes
                        const encoder = new TextEncoder();
                        const fallbackBytes = encoder.encode(String(value));
                        builder.adduint8Array(fallbackBytes);
                    }
                }
            } else {
                console.error(`  ❌ Missing value for field: ${field.name}`);
                throw new Error(`Missing value for required field: ${field.name}`);
            }
        });

        const payload = new DynamicPayload(method.packageSize);
        payload.setPayload(builder.getData());

        return payload;
    }

    /**
     * Crea transacción Qubic
     */
    private createQubicTransaction(
        senderId: string,
        senderSeed: string,
        contractAddress: string,
        method: ContractMethod,
        payload: DynamicPayload,
        targetTick: number,
    ): Observable<QubicTransaction> {
        return from(
            (async (): Promise<QubicTransaction> => {
                try {
                    const transaction = new QubicTransaction()
                        .setSourcePublicKey(new PublicKey(senderId))
                        .setDestinationPublicKey(new PublicKey(contractAddress))
                        .setTick(targetTick)
                        .setInputSize(payload.getPackageSize())
                        .setPayload(payload);

                    if (method.type === 'FUNCTION') {
                        if (method.procedureIndex === undefined) {
                            throw new Error(`Function ${method.name} missing procedureIndex`);
                        }
                        transaction.setInputType(method.procedureIndex);
                        transaction.setAmount(new Long(BigInt(0)));
                        console.log(`📞 Function call - InputType: ${method.procedureIndex}`);
                    } else {
                        // PROCEDURE
                        const inputType = method.procedureIndex || 1;
                        transaction.setInputType(inputType);
                        console.log(`🔧 Procedure call - InputType: ${inputType}`);

                        const feeAmount = this.extractFeeAmount(method.fees);
                        transaction.setAmount(new Long(BigInt(feeAmount)));
                        console.log(`💰 Amount: ${feeAmount}`);
                    }

                    await transaction.build(senderSeed);
                    console.log(`✅ Transaction created successfully for ${method.name}`);

                    return transaction;
                } catch (error) {
                    console.error(`Error creating transaction for ${method.name}:`, error);
                    throw error;
                }
            })(),
        );
    }

    /**
     * Extrae el amount de las fees de manera segura
     */
    private extractFeeAmount(fees: Record<string, any>): number {
        if (!fees || typeof fees !== 'object') {
            return 0;
        }

        const requiresFee = fees['requiresFee'];
        if (!requiresFee) {
            return 0;
        }

        const amount = fees['amount'];
        if (typeof amount === 'number') {
            return amount;
        }

        if (typeof amount === 'string') {
            const parsed = parseInt(amount, 10);
            return isNaN(parsed) ? 0 : parsed;
        }

        return 0;
    }

    /**
     * Obtiene tick actual
     */
    private getCurrentTick(apiUrl: string): Observable<number> {
        return this.http.get<TickInfoResponse>(`${apiUrl}/v1/tick-info`).pipe(
            map((response: TickInfoResponse) => {
                if (response && response.tickInfo && typeof response.tickInfo.tick === 'number') {
                    return response.tickInfo.tick;
                }
                throw new Error('Invalid response format from status endpoint');
            }),
            catchError((error: Error) => {
                console.error('Error getting current tick:', error);
                throw error;
            }),
        );
    }

    /**
     * Envía transacción
     */
    private broadcastTransaction(transaction: QubicTransaction, apiUrl: string): Observable<BroadcastResponse> {
        try {
            const encodedTransaction = transaction.encodeTransactionToBase64(transaction.getPackageData());

            const request: BroadcastRequest = {
                encodedTransaction: encodedTransaction,
            };

            return this.http.post<BroadcastResponse>(`${apiUrl}/v1/broadcast-transaction`, request).pipe(
                catchError((error: Error) => {
                    console.error('Broadcast error:', error);
                    throw error;
                }),
            );
        } catch (error) {
            console.error('Error encoding transaction:', error);
            throw error;
        }
    }

    /**
     * Crea resultado exitoso
     */
    private createSuccessResult(
        testCase: SecurityTestCase,
        method: ContractMethod,
        transaction: QubicTransaction,
        payload: DynamicPayload,
        broadcastResult: BroadcastResponse,
    ): TestExecutionResult {
        const vulnerabilityConfirmed = this.assessVulnerability(testCase, 'success', broadcastResult);

        const testInputs = this.parseTestInputs(testCase.testInputs);

        return {
            testCase,
            method,
            transaction,
            payload,
            executionStatus: 'success',
            broadcastResult,
            actualBehavior: 'Transaction accepted by network',
            securityAssessment: {
                vulnerabilityConfirmed,
                riskLevel: vulnerabilityConfirmed ? testCase.severity : 'Mitigated',
                notes: this.generateAssessmentNotes(testCase, vulnerabilityConfirmed),
            },
        };
    }

    /**
     * Crea resultado de error
     */
    private createErrorResult(testCase: SecurityTestCase, method: ContractMethod, error: string): TestExecutionResult {
        const vulnerabilityMitigated = error.includes('validation') || error.includes('invalid') || error.includes('bounds') || error.includes('range');

        return {
            testCase,
            method,
            executionStatus: 'error',
            error,
            actualBehavior: `Error occurred: ${error}`,
            securityAssessment: {
                vulnerabilityConfirmed: !vulnerabilityMitigated,
                riskLevel: vulnerabilityMitigated ? 'Mitigated' : testCase.severity,
                notes: this.generateAssessmentNotes(testCase, !vulnerabilityMitigated, error),
            },
        };
    }

    /**
     * Evalúa si la vulnerabilidad fue confirmada
     */
    private assessVulnerability(testCase: SecurityTestCase, status: string, broadcastResult?: BroadcastResponse): boolean {
        if (status === 'success') {
            return true;
        }

        return false;
    }

    /**
     * Genera notas de evaluación
     */
    private generateAssessmentNotes(testCase: SecurityTestCase, vulnerabilityConfirmed: boolean, error?: string): string {
        const testInputs = this.parseTestInputs(testCase.testInputs);

        let notes = `🧪 Test: ${testCase.testName}\n`;
        notes += `🎯 Target Variable: ${testCase.targetVariable}\n`;
        notes += `💀 Malicious Value: ${testInputs?.targetInput?.maliciousValue || 'unknown'}\n`;
        notes += `📝 Attack Reason: ${testInputs?.targetInput?.attackReason || 'unknown'}\n\n`;

        if (error) {
            notes += `❌ Error: ${error}\n\n`;
        }

        if (vulnerabilityConfirmed) {
            notes += `🚨 VULNERABILITY CONFIRMED: ${testCase.vulnerabilityType}\n`;
            notes += `💥 Risk: ${testCase.actualRisk}\n`;
            notes += `🛡️ Recommended mitigations:\n`;
            if (testCase.mitigationSteps && Array.isArray(testCase.mitigationSteps)) {
                testCase.mitigationSteps.forEach((step: string) => {
                    notes += `   • ${step}\n`;
                });
            }
        } else {
            notes += `✅ VULNERABILITY MITIGATED: Security controls working correctly\n`;
            if (error) {
                notes += `Contract properly rejected malicious input with error validation\n`;
            }
        }

        return notes;
    }

    /**
     * Genera reporte final de resultados
     */
    generateSecurityReport(results: TestExecutionResult[]): {
        executionDate: string;
        summary: {
            totalTests: number;
            vulnerabilitiesConfirmed: number;
            vulnerabilitiesMitigated: number;
            errors: number;
            byVulnerabilityType: Record<string, number>;
            bySeverity: Record<string, number>;
        };
        detailedResults: Array<{
            testName: string;
            method: string;
            targetVariable: string;
            maliciousValue: string;
            vulnerabilityType: string;
            severity: string;
            status: ExecutionStatus;
            vulnerabilityConfirmed: boolean;
            riskLevel: string;
            actualBehavior?: string;
            notes: string;
        }>;
    } {
        const summary = {
            totalTests: results.length,
            vulnerabilitiesConfirmed: results.filter((r) => r.securityAssessment.vulnerabilityConfirmed).length,
            vulnerabilitiesMitigated: results.filter((r) => !r.securityAssessment.vulnerabilityConfirmed).length,
            errors: results.filter((r) => r.executionStatus === 'error').length,
            byVulnerabilityType: this.groupBy(results, (r) => r.testCase.vulnerabilityType),
            bySeverity: this.groupBy(
                results.filter((r) => r.securityAssessment.vulnerabilityConfirmed),
                (r) => r.testCase.severity,
            ),
        };

        return {
            executionDate: new Date().toISOString(),
            summary,
            detailedResults: results.map((r) => {
                const testInputs = this.parseTestInputs(r.testCase.testInputs);
                return {
                    testName: r.testCase.testName,
                    method: r.method.name,
                    targetVariable: r.testCase.targetVariable,
                    maliciousValue: testInputs?.targetInput?.maliciousValue || 'unknown',
                    vulnerabilityType: r.testCase.vulnerabilityType,
                    severity: r.testCase.severity,
                    status: r.executionStatus,
                    vulnerabilityConfirmed: r.securityAssessment.vulnerabilityConfirmed,
                    riskLevel: r.securityAssessment.riskLevel,
                    actualBehavior: r.actualBehavior,
                    notes: r.securityAssessment.notes,
                };
            }),
        };
    }

    private groupBy<T>(array: T[], keyFn: (item: T) => string): Record<string, number> {
        return array.reduce(
            (acc, item) => {
                const key = keyFn(item);
                acc[key] = (acc[key] || 0) + 1;
                return acc;
            },
            {} as Record<string, number>,
        );
    }
}
