import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Contract } from '../shared/models/contract.models';
import { AnalyzeContractRequest } from '../shared/models/requests.models';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { ContractAnalysis } from '../shared/models/contract-analysis.model';
import { TestExecutionResult } from '../shared/models/test-execution-result.model';
import { SecurityTestCase } from '../shared/models/security-test-case.model';

@Injectable({
    providedIn: 'root',
})
export class ContractService {
    mockContract: Contract = {
        id: 'mock-id',
        fileName: 'Mock Contract',
        contractCode: '0x000123',
        summary:
            'The contract implements a basic ERC-20 token system with standard functionality. Three public methods and two potential minor vulnerabilities related to input validation were detected.',
        vulnerabilities: ['Reentrancy', 'Integer Overflow'],
        methods: [
            {
                id: 'method-1',
                contractId: 'mock-id',
                name: 'transfer',
                signature: 'transfer(address,uint256):bool',
                description: 'Transfiere tokens a una dirección específica.',
                variables: [
                    { id: 'var-1', methodId: 'method-1', position: 0, name: 'to', type: 'address', description: 'Dirección de destino' },
                    { id: 'var-2', methodId: 'method-1', position: 1, name: 'amount', type: 'uint256', description: 'Cantidad a transferir' },
                ],
                testCases: [
                    {
                        id: 'tc-1',
                        methodId: 'method-1',
                        description: 'Transferencia válida',
                        riskLevel: 'low',
                        category: 'logic',
                        testValues: [],
                        result: { id: '1', executionTime: 100, testCaseId: 'tc-1', status: 'pending' },
                    },
                ],
            },
            {
                id: 'method-2',
                contractId: 'mock-id',
                name: 'balanceOf',
                signature: 'balanceOf(address):uint256',
                description: 'Devuelve el balance de una dirección.',
                variables: [{ id: 'var-3', methodId: 'method-2', position: 0, name: 'owner', type: 'address', description: 'Dirección del propietario' }],
                testCases: [
                    {
                        id: 'tc-2',
                        methodId: 'method-2',
                        description: 'Consulta de balance',
                        riskLevel: 'low',
                        category: 'logic',
                        testValues: [],
                        result: { id: '1', executionTime: 100, testCaseId: 'tc-2', status: 'success' },
                    },
                ],
            },
            {
                id: 'method-3',
                contractId: 'mock-id',
                name: 'approve',
                signature: 'approve(address,uint256):bool',
                description: 'Permite a una dirección gastar una cantidad específica de tokens en nombre del propietario.',
                variables: [
                    { id: 'var-4', methodId: 'method-3', position: 0, name: 'spender', type: 'address', description: 'Dirección autorizada' },
                    { id: 'var-5', methodId: 'method-3', position: 1, name: 'amount', type: 'uint256', description: 'Cantidad permitida' },
                ],
                testCases: [
                    {
                        id: 'tc-3',
                        methodId: 'method-3',
                        description: 'Aprobación válida de gasto',
                        riskLevel: 'medium',
                        category: 'access',
                        testValues: [],
                        result: { id: '2', executionTime: 120, testCaseId: 'tc-3', status: 'pending' },
                    },
                ],
            },
        ],
        fakeContractAddress: '0x1234567890abcdef1234567890abcdef12345678',
        fakeTransactionHash: '0xabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdef',
        createdAt: new Date(),
    };

    private apiUrl = `${environment.apiUrl}contracts`;

    constructor(private http: HttpClient) {}

    analyzeMockContract(): Observable<Contract> {
        // Mock implementation

        return new Observable<Contract>((observer) => {
            setTimeout(() => {
                observer.next(this.mockContract);
                observer.complete();
            }, 2000);
        });
    }

    analyzeContract(request: AnalyzeContractRequest): Observable<ContractAnalysis> {
        const formData = new FormData();
        formData.append('file', request.file);

        return this.http.post<ContractAnalysis>(`${this.apiUrl}/analyze`, formData);
    }

    saveExecutionResults(result: TestExecutionResult): Observable<any> {
        const payload = {
            testCaseId: result.testCase.id, // Asume que SecurityTestCase tiene un id
            executionResult: this.sanitizeExecutionResultForSave(result),
        };

        return this.http.post<SecurityTestCase>(`${this.apiUrl}/save-execution-result`, payload);
    }

    private sanitizeExecutionResultForSave(result: TestExecutionResult): any {
        debugger;
        return {
            executionStatus: result.executionStatus,
            actualBehavior: result.actualBehavior,
            error: result.error,
            securityAssessment: {
                vulnerabilityConfirmed: result.securityAssessment.vulnerabilityConfirmed,
                riskLevel: result.securityAssessment.riskLevel,
                notes: result.securityAssessment.notes,
            },
            // Para broadcast results
            broadcastResult: result.broadcastResult
                ? {
                      peersBroadcasted: result.broadcastResult.peersBroadcasted,
                      encodedTransaction: result.broadcastResult.encodedTransaction,
                      transactionId: result.broadcastResult.transactionId,
                  }
                : null,
            // Para query results
            queryResult: result.queryResult
                ? {
                      responseData: result.queryResult.responseData,
                      success: result.queryResult.success,
                      error: result.queryResult.error,
                  }
                : null,
            // Información básica de la transacción (sin objetos complejos)
            // transactionInfo: result.transaction ? result.transaction : null,
            // // Información del payload
            // payloadInfo: result.payload
            //     ? {
            //           packageSize: result.payload.getPackageSize(),
            //           // No incluir los datos binarios completos para ahorrar espacio
            //       }
            //     : null,
            executedAt: new Date().toISOString(),
        };
    }

    deployContract(contractId: string): Observable<any> {
        // Mock implementation
        return new Observable<any>((observer) => {
            setTimeout(() => {
                observer.next({ success: true, contractId });
                observer.complete();
            }, 500);
        });
    }

    generateTestCases(contractId: string): Observable<Contract> {
        // Mock implementation

        return new Observable<Contract>((observer) => {
            setTimeout(() => {
                observer.next(this.mockContract);
                observer.complete();
            }, 500);
        });
    }

    getContract(contractId: string): Observable<ContractAnalysis> {
        return this.http.get<ContractAnalysis>(`${this.apiUrl}/${contractId}`);
    }

    downloadContract(contractId: string) {
        this.getContract(contractId).subscribe({
            next: (contractAnalysis) => {
                const jsonString = JSON.stringify(contractAnalysis, null, 2);
                const blob = new Blob([jsonString], { type: 'application/json' });
                const url = window.URL.createObjectURL(blob);

                const link = document.createElement('a');
                link.href = url;
                link.download = `full-report-${contractAnalysis.contractName}.json`;
                link.click();

                window.URL.revokeObjectURL(url);
            },
        });
    }
}
