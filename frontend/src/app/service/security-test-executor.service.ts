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
import { encodeParams } from './contractUtils';

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
import { environment } from 'src/environments/environment';
import { QuerySmartContractResponse } from '../shared/models/query-smart-contract-response';
import { QuerySmartContractRequest } from '../shared/models/query-smart-contract-request';
import { TestExecutionResult } from '../shared/models/test-execution-result.model';
import { Buffer } from 'buffer';

type ExecutionStatus = 'success' | 'failed' | 'rejected' | 'error';

@Injectable({
    providedIn: 'root',
})
export class SecurityTestExecutorService {
    executionconfig: TestExecutionConfig = <TestExecutionConfig>{
        qubicRpcUrl: environment.qubicRpcUrl,
        contractAddress: environment.contractAddress,
        testIdentity: environment.testIdentity,
        testSeed: environment.testSeed,
        tickOffset: environment.tickOffset,
        delayBetweenTests: environment.delayBetweenTests,
    };
    constructor(private http: HttpClient) {}

    /**
     * Ejecuta todos los casos de prueba usando los valores reales de Groq
     */
    executeAllSecurityTests(contractAnalysis: ContractAnalysis): Observable<TestExecutionResult[]> {
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
            this.executeSecurityTest(testData.testCase, testData.method).pipe(
                delay(index * this.executionconfig.delayBetweenTests),
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
    public executeSecurityTest(testCase: SecurityTestCase, method: ContractMethod): Observable<TestExecutionResult> {
        console.log(`🔬 Executing: ${testCase.testName}`);

        const testInputs = this.parseTestInputs(testCase.testInputs);
        console.log(`🎯 Target: ${testCase.targetVariable} with value: ${testInputs?.targetInput?.maliciousValue}`);

        return this.getCurrentTick(this.executionconfig.qubicRpcUrl).pipe(
            switchMap((currentTick: number) => {
                try {
                    // 1. Preparar inputs con valores específicos de Groq
                    const transactionInputs = this.prepareInputsFromTestCase(testCase, method, testInputs);

                    // 2. Crear payload dinámico
                    const payload = this.createDynamicPayload(method, transactionInputs);

                    // 3. Crear transacción
                    const targetTick = currentTick + this.executionconfig.tickOffset;

                    if (method.type === 'FUNCTION') {
                        // Para funciones, usar querySmartContract (no necesita tick ni transacción)
                        console.log(`🔍 Executing FUNCTION ${method.name} via querySmartContract`);
                        return this.querySmartContract(method, transactionInputs).pipe(
                            map((result: QuerySmartContractResponse) => this.createSuccessResultFromQuery(testCase, method, transactionInputs, result)),
                            catchError((error: Error) => {
                                console.error(`Query failed for ${testCase.testName}:`, error);
                                return from([this.createErrorResult(testCase, method, `Query failed: ${error.message}`)]);
                            }),
                        );
                    } else {
                        // Para procedures, usar broadcastTransaction (código original)
                        console.log(`🔧 Executing PROCEDURE ${method.name} via broadcastTransaction`);
                        return this.createQubicTransaction(
                            this.executionconfig.testIdentity,
                            this.executionconfig.testSeed,
                            this.executionconfig.contractAddress,
                            method,
                            payload,
                            targetTick,
                        ).pipe(
                            switchMap((transaction: QubicTransaction) => {
                                return this.broadcastTransaction(transaction, this.executionconfig.qubicRpcUrl).pipe(
                                    map((result: BroadcastResponse) => this.createSuccessResult(testCase, method, transaction, payload, result)),
                                    catchError((error: Error) => {
                                        console.error(`Broadcast failed for ${testCase.testName}:`, error);
                                        return from([this.createErrorResult(testCase, method, `Broadcast failed: ${error.message}`)]);
                                    }),
                                );
                            }),
                        );
                    }

                    // return this.createQubicTransaction(
                    //     this.executionconfig.testIdentity,
                    //     this.executionconfig.testSeed,
                    //     this.executionconfig.contractAddress,
                    //     method,
                    //     payload,
                    //     targetTick,
                    // ).pipe(
                    //     switchMap((transaction: QubicTransaction) => {
                    //         return this.broadcastTransaction(transaction, this.executionconfig.qubicRpcUrl).pipe(
                    //             map((result: BroadcastResponse) => this.createSuccessResult(testCase, method, transaction, payload, result)),
                    //             catchError((error: Error) => {
                    //                 console.error(`Broadcast failed for ${testCase.testName}:`, error);
                    //                 return from([this.createErrorResult(testCase, method, `Broadcast failed: ${error.message}`)]);
                    //             }),
                    //         );
                    //     }),
                    // );
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

    private createSuccessResultFromQuery(
        testCase: SecurityTestCase,
        method: ContractMethod,
        inputs: Record<string, any>,
        queryResult: QuerySmartContractResponse,
    ): TestExecutionResult {
        const vulnerabilityConfirmed = this.assessVulnerabilityFromQuery(testCase, queryResult);

        return {
            testCase,
            method,
            executionStatus: 'success',
            queryResult, // Agregar este campo a TestExecutionResult interface si no existe
            actualBehavior: `Query executed successfully. Response: ${JSON.stringify(queryResult).substring(0, 200)}...`,
            securityAssessment: {
                vulnerabilityConfirmed,
                riskLevel: vulnerabilityConfirmed ? testCase.severity : 'Mitigated',
                notes: this.generateAssessmentNotesForQuery(testCase, vulnerabilityConfirmed, queryResult),
            },
        };
    }

    private assessVulnerabilityFromQuery(testCase: SecurityTestCase, queryResult: QuerySmartContractResponse): boolean {
        // Si hay error explícito, probablemente la vulnerabilidad está mitigada
        if (queryResult.error) {
            console.log(`🛡️ Query returned error: ${queryResult.error} - vulnerability likely mitigated`);
            return false;
        }

        // Si success es explícitamente false
        if (queryResult.success === false) {
            console.log(`🛡️ Query success=false - vulnerability likely mitigated`);
            return false;
        }

        // Si no hay responseData, podría indicar que la función no devolvió datos por validaciones
        if (!queryResult.responseData) {
            console.log(`🛡️ No responseData - function may have rejected malicious input`);
            return false;
        }

        // Si la query fue exitosa y devolvió datos, podría indicar una vulnerabilidad
        // dependiendo del tipo de test
        if (queryResult.responseData) {
            console.log(`🚨 Query returned data: ${queryResult.responseData.substring(0, 50)}... - analyzing for vulnerability`);

            // Aquí puedes implementar lógica específica según el tipo de vulnerabilidad
            switch (testCase.vulnerabilityType) {
                case 'Information Disclosure':
                    // Si devuelve información que no debería, es vulnerable
                    return true;

                case 'Access Control':
                    // Si permite acceso cuando no debería, es vulnerable
                    return true;

                case 'Input Validation':
                    // Si procesa input malicioso sin error, es vulnerable
                    return true;

                default:
                    // Por defecto, si la query es exitosa, asumimos vulnerabilidad
                    return true;
            }
        }

        return false;
    }

    private generateAssessmentNotesForQuery(testCase: SecurityTestCase, vulnerabilityConfirmed: boolean, queryResult: QuerySmartContractResponse): string {
        const testInputs = this.parseTestInputs(testCase.testInputs);

        let notes = `🔍 Query Test: ${testCase.testName}\n`;
        notes += `🎯 Target Variable: ${testCase.targetVariable}\n`;

        if (testInputs?.targetInput) {
            notes += `💀 Malicious Value: ${testInputs.targetInput.maliciousValue}\n`;
            notes += `📝 Attack Reason: ${testInputs.targetInput.attackReason}\n\n`;
        } else {
            notes += `📭 Internal Logic Test (no external inputs)\n`;
            notes += `🔍 Testing: ${testCase.description}\n\n`;
        }

        // Información sobre la respuesta de la query
        if (queryResult.error) {
            notes += `❌ Query Error: ${queryResult.error}\n`;
        } else if (queryResult.responseData) {
            notes += `📊 Query Response Data: ${queryResult.responseData.substring(0, 100)}...\n`;
        } else {
            notes += `📭 No response data returned\n`;
        }

        if (vulnerabilityConfirmed) {
            notes += `\n🚨 VULNERABILITY CONFIRMED: ${testCase.vulnerabilityType}\n`;
            notes += `💥 Risk: ${testCase.actualRisk}\n`;
            notes += `🔍 Function returned data when it should have rejected malicious input\n`;
            notes += `🛡️ Recommended mitigations:\n`;
            if (testCase.mitigationSteps && Array.isArray(testCase.mitigationSteps)) {
                testCase.mitigationSteps.forEach((step: string) => {
                    notes += `   • ${step}\n`;
                });
            }
        } else {
            notes += `\n✅ VULNERABILITY MITIGATED: Security controls working correctly\n`;
            notes += `🛡️ Function properly rejected or handled malicious input\n`;
        }

        return notes;
    }

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

    private prepareInputsFromTestCase(
        testCase: SecurityTestCase,
        method: ContractMethod,
        testInputs: TestInputs | null,
    ): Record<string, PublicKey | Long | boolean | string | any[]> {
        // Si no hay testInputs o no hay inputFields, retornar objeto vacío
        if (!testInputs || !method.inputFields || method.inputFields.length === 0) {
            console.log(`📭 Method ${method.name} has no input parameters - using empty inputs`);
            return {};
        }

        // Si testInputs existe pero no tiene targetInput, también retornar vacío
        if (!testInputs.targetInput) {
            console.log(`📭 Test case ${testCase.testName} has no targetInput - using empty inputs`);
            return {};
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

    private createDynamicPayload(method: ContractMethod, inputs: Record<string, PublicKey | Long | boolean | string | any[]>): DynamicPayload {
        console.log(`📦 Creating payload for ${method.name} (${method.packageSize} bytes)`);

        // Si packageSize es 0 o no hay inputFields, crear payload vacío
        if (method.packageSize === 0 || !method.inputFields || method.inputFields.length === 0) {
            console.log(`📭 Empty payload for method ${method.name} (no input parameters)`);
            const payload = new DynamicPayload(0);
            payload.setPayload(new Uint8Array(0));
            return payload;
        }

        // Si inputs está vacío pero se esperan fields, también crear payload vacío
        if (Object.keys(inputs).length === 0) {
            console.log(`📭 No inputs provided, creating empty payload`);
            const payload = new DynamicPayload(0);
            payload.setPayload(new Uint8Array(0));
            return payload;
        }

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
                        .setInputSize(payload.getPackageSize()) // Esto será 0 para métodos sin parámetros
                        .setPayload(payload);

                    if (method.type === 'FUNCTION') {
                        if (method.procedureIndex === undefined) {
                            throw new Error(`Function ${method.name} missing procedureIndex`);
                        }
                        transaction.setInputType(method.procedureIndex);
                        transaction.setAmount(new Long(BigInt(0)));
                        console.log(`📞 Function call - InputType: ${method.procedureIndex}, InputSize: ${payload.getPackageSize()}`);
                    } else {
                        const inputType = method.procedureIndex || 1;
                        transaction.setInputType(inputType);
                        console.log(`🔧 Procedure call - InputType: ${inputType}, InputSize: ${payload.getPackageSize()}`);

                        const feeAmount = this.extractFeeAmount(method.fees);
                        transaction.setAmount(new Long(BigInt(feeAmount)));
                        console.log(`💰 Amount: ${feeAmount}`);
                    }

                    await transaction.build(senderSeed);
                    console.log(`✅ Transaction created successfully for ${method.name} (payload size: ${payload.getPackageSize()})`);

                    return transaction;
                } catch (error) {
                    console.error(`Error creating transaction for ${method.name}:`, error);
                    throw error;
                }
            })(),
        );
    }

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

    private mapQubicTypeToEncodeType(qubicType: string): string {
        const typeMap: Record<string, string> = {
            uint8: 'uint8',
            uint16: 'uint16',
            uint32: 'uint32',
            uint64: 'uint64',
            int8: 'int8',
            int16: 'int16',
            int32: 'int32',
            int64: 'int64',
            sint8: 'sint8',
            sint16: 'sint16',
            sint32: 'sint32',
            sint64: 'sint64',
            PublicKey: 'id',
            boolean: 'bit',
            string: 'char[64]', // Default size, ajustar según necesidad
            bit: 'bit',
            bool: 'bit',
        };

        return typeMap[qubicType] || qubicType;
    }

    private querySmartContract(
        method: ContractMethod,
        inputs: Record<string, PublicKey | Long | boolean | string | any[]>,
    ): Observable<QuerySmartContractResponse> {
        // Crear requestData como base64 encoded usando encodeParams
        const requestData = this.createRequestDataFromInputs(inputs, method);

        // Calcular inputSize basado en el requestData codificado
        const inputSize = requestData ? Buffer.from(requestData, 'base64').length : 0;

        const request: QuerySmartContractRequest = {
            contractIndex: environment.smartContractIndex, // Asegúrate de tener esto en environment
            inputType: method.procedureIndex || 1,
            inputSize: inputSize,
            requestData: requestData, // Base64 encoded string
        };

        console.log(`🔍 Querying smart contract for function ${method.name}:`, request);

        return this.http.post<QuerySmartContractResponse>(`${this.executionconfig.qubicRpcUrl}/v1/querySmartContract`, request).pipe(
            catchError((error: Error) => {
                console.error('Query smart contract error:', error);
                throw error;
            }),
        );
    }

    /**
     * Convierte los inputs a requestData usando la función encodeParams del contractUtils
     * Para querySmartContract, el requestData debe ser base64 encoded como en broadcastTransaction
     */
    private createRequestDataFromInputs(inputs: Record<string, PublicKey | Long | boolean | string | any[]>, method: ContractMethod): string {
        if (!method.inputFields || method.inputFields.length === 0) {
            return '';
        }

        // Convertir inputs de tipos Qubic a valores primitivos para encodeParams
        const primitiveInputs: Record<string, any> = {};

        method.inputFields.forEach((field: ContractField) => {
            const value = inputs[field.name];
            if (value !== undefined) {
                if (value instanceof PublicKey) {
                    // Para PublicKey, usar el método apropiado o convertir a string ID
                    primitiveInputs[field.name] = value.getIdentityAsSring ? value.getIdentityAsSring() : value.toString();
                } else if (value instanceof Long) {
                    // Para Long, obtener el valor numérico
                    primitiveInputs[field.name] = value.getNumber ? value.getNumber().toString() : value.toString();
                } else if (typeof value === 'boolean') {
                    primitiveInputs[field.name] = value;
                } else if (typeof value === 'string' || typeof value === 'number') {
                    primitiveInputs[field.name] = value;
                } else if (Array.isArray(value)) {
                    primitiveInputs[field.name] = value;
                } else {
                    primitiveInputs[field.name] = String(value);
                }

                console.log(`📋 Primitive input ${field.name}: ${primitiveInputs[field.name]} (${typeof primitiveInputs[field.name]})`);
            }
        });

        // Convertir ContractField[] a formato esperado por encodeParams
        const inputFieldsForEncode = method.inputFields.map((field: ContractField) => {
            const mappedField: any = {
                name: field.name,
                type: this.mapQubicTypeToEncodeType(field.qubicType),
            };

            // Si es un array, configurar como Array con elementType y size
            if (field.isArray) {
                // Para arrays, extraer el tipo de elemento del qubicType
                // Ejemplo: "Array<uint64, 16>" o "uint64[16]"
                if (field.qubicType.includes('Array<')) {
                    const arrayMatch = field.qubicType.match(/Array<([^,]+),\s*([^>]+)>/);
                    if (arrayMatch) {
                        mappedField.type = 'Array';
                        mappedField.elementType = this.mapQubicTypeToEncodeType(arrayMatch[1].trim());
                        mappedField.size = arrayMatch[2].trim();
                    }
                } else if (field.qubicType.includes('[')) {
                    // Formato como "uint64[16]"
                    const arrayMatch = field.qubicType.match(/([^\[]+)\[([^\]]+)\]/);
                    if (arrayMatch) {
                        mappedField.type = 'Array';
                        mappedField.elementType = this.mapQubicTypeToEncodeType(arrayMatch[1].trim());
                        mappedField.size = arrayMatch[2].trim();
                    }
                } else if (field.arraySize) {
                    // Fallback: usar arraySize si está disponible
                    mappedField.type = 'Array';
                    mappedField.elementType = this.mapQubicTypeToEncodeType(field.qubicType);
                    mappedField.size = field.arraySize.toString();
                }
            }
            // Si es char array (string), mantener el formato completo
            else if (field.qubicType.includes('char[')) {
                // Mantener como "char[64]" para que encodeParams lo maneje correctamente
                mappedField.type = field.qubicType;
            }
            // Para otros tipos, usar el tamaño si está disponible
            else if (field.byteSize && field.byteSize > 0) {
                mappedField.size = field.byteSize;
            }

            console.log(
                `🗂️ Field mapping: ${field.name} (${field.qubicType}) -> type: ${mappedField.type}, elementType: ${mappedField.elementType || 'N/A'}, size: ${mappedField.size || 'N/A'}`,
            );

            return mappedField;
        });

        // Usar encodeParams del contractUtils
        debugger;
        const encodedData = encodeParams(primitiveInputs, inputFieldsForEncode);

        console.log(`📦 Encoded data length: ${encodedData.length}`);

        return encodedData;
    }

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

    private generateAssessmentNotes(testCase: SecurityTestCase, vulnerabilityConfirmed: boolean, error?: string): string {
        const testInputs = this.parseTestInputs(testCase.testInputs);

        let notes = `🧪 Test: ${testCase.testName}\n`;
        notes += `🎯 Target Variable: ${testCase.targetVariable}\n`;

        if (testInputs?.targetInput) {
            notes += `💀 Malicious Value: ${testInputs.targetInput.maliciousValue}\n`;
            notes += `📝 Attack Reason: ${testInputs.targetInput.attackReason}\n\n`;
        } else {
            notes += `📭 Internal Logic Test (no external inputs)\n`;
            notes += `🔍 Testing: ${testCase.description}\n\n`;
        }

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
                    maliciousValue: testInputs?.targetInput?.maliciousValue || 'N/A (Internal Logic Test)',
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
