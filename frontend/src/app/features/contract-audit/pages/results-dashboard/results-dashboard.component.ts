import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { colDef } from '@bhplugin/ng-datatable';
import { ContractService } from 'src/app/service/contract.service';
import { SecurityTestExecutorService } from 'src/app/service/security-test-executor.service';
import { ContractAnalysis } from 'src/app/shared/models/contract-analysis.model';
import { ContractMethod } from 'src/app/shared/models/contract-method.models';
import { SecurityTestCase } from 'src/app/shared/models/security-test-case.model';

interface ContractMethodUI {
    method: ContractMethod;
    testCases: SecurityTestCase[];
    subtitle: string;
}

@Component({
    selector: 'app-results-dashboard',
    templateUrl: './results-dashboard.component.html',
})
export class ResultsDashboardComponent {
    contractId: string | null = null;
    contractAnalysis: ContractAnalysis | null = null;
    contractMethods: ContractMethodUI[] = [];
    cols: Array<colDef> = [];
    totalCases: number = 0;
    testCasesExecuted: number = 0;
    showModal = false;
    selectedSecurityTestCase: SecurityTestCase | null = null;

    constructor(
        private route: ActivatedRoute,
        private contractService: ContractService,
        private securityTestExecutorService: SecurityTestExecutorService,
    ) {
        this.contractId = this.route.snapshot.paramMap.get('contractId');

        this.initDatatableColumns();
    }

    ngOnInit() {
        if (this.contractId) {
            console.log('Fetching contract analysis for ID:', this.contractId);
            this.contractService.getContract(this.contractId).subscribe({
                next: (contractAnalysis) => {
                    this.contractAnalysis = contractAnalysis;
                    this.createContractMethodsUI();
                    this.getTotalCases();
                    this.executeCases();
                },
                error: (error) => {
                    console.error('Error generating test cases:', error);
                },
            });
        }
    }

    initDatatableColumns() {
        this.cols = [
            { field: 'id', title: 'ID', filter: false, hide: true },
            { field: 'testName', title: 'Test Name' },
            { field: 'actualRisk', title: 'Actual Risk' },
            { field: 'description', title: 'Description' },
            { field: 'vulnerabilityType', title: 'Vulnerability Type' },
            { field: 'lastExecutedAt', title: 'Last Executed At', hide: true },
            { field: 'status', title: 'Status' },
            { field: 'action', title: 'Action', sort: false, headerClass: 'justify-center' },
        ];
    }

    getTotalCases() {
        this.contractMethods.forEach((methodUI) => {
            this.totalCases += methodUI.testCases.length;
            this.testCasesExecuted += methodUI.testCases.filter((tc) => tc.lastExecutedAt).length;
        });
    }

    async executeCases() {
        this.testCasesExecuted = 0;
        for (const methodUI of this.contractMethods) {
            for (const testCase of methodUI.testCases) {
                try {
                    const result = await this.securityTestExecutorService.executeSecurityTest(testCase, methodUI.method).toPromise();
                    if (result !== undefined) {
                        this.testCasesExecuted++;
                        const saveResult = await this.contractService.saveExecutionResults(result).toPromise();

                        methodUI.testCases = methodUI.testCases.map((tc) => (tc.id === saveResult.id ? saveResult : tc));
                    } else {
                        console.warn('Test execution result is undefined for testCase:', testCase);
                    }
                } catch (error) {
                    console.error('Error executing test:', error);
                }
            }
        }
    }
    // executeCases() {

    //     this.testCasesExecuted = 0;
    //     this.contractMethods.forEach((methodUI) => {
    //         methodUI.testCases.forEach((testCase) => {
    //             this.securityTestExecutorService.executeSecurityTest(testCase, methodUI.method).subscribe({
    //                 next: (result) => {
    //                     this.testCasesExecuted++;
    //                     this.contractService.saveExecutionResults(result).subscribe({
    //                         next: (saveResult) => {
    //                             testCase = saveResult;
    //                             this.contractMethods = this.contractMethods.map((m) => {
    //                                 if (m.method.id === methodUI.method.id) {
    //                                     return {
    //                                         ...m,
    //                                         testCases: m.testCases.map((tc) => (tc.id === testCase.id ? testCase : tc)),
    //                                     };
    //                                 } else {
    //                                     return { ...m };
    //                                 }
    //                             });
    //                         },
    //                     });
    //                     console.log('All tests executed successfully:', result);
    //                 },
    //                 error: (error) => {
    //                     console.error('Error executing all tests:', error);
    //                 },
    //             });
    //         });
    //     });
    // }

    createContractMethodsUI() {
        this.contractMethods = this.contractAnalysis?.methods
            .sort((a, b) => {
                if (a.type !== b.type) {
                    if (a.type === 'PROCEDURE') return -1;
                    if (b.type === 'PROCEDURE') return 1;
                    return 0;
                }

                return a.procedureIndex! - b.procedureIndex!;
            })
            .map((method) => {
                return <ContractMethodUI>{
                    method: method,
                    subtitle: this.getCardSubtitle(method),
                    testCases: this.getSecurityTestCases(method),
                };
            })!;
    }

    getCardSubtitle(method: ContractMethod): string {
        return `${method.name}(${method.inputFields.map((f) => `${f.name} ${f.qubicType}`).join(', ')}) : ${method.outputFields.map((f) => `${f.name} ${f.qubicType}`).join(', ')}`;
    }

    getSecurityTestCases(method: ContractMethod): SecurityTestCase[] {
        return this.contractAnalysis?.securityAudit?.securityTests.filter((testCase) => testCase.methodName === method.name) || [];
    }

    showDetails(testCase: SecurityTestCase) {
        this.selectedSecurityTestCase = testCase;
        this.showModal = true;
    }
}
