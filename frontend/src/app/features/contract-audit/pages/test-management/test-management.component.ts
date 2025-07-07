import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
    selector: 'app-test-management',
    templateUrl: './test-management.component.html',
})
export class TestManagementComponent {
    contractId: string | null = null;
    contractAnalysis: ContractAnalysis | null = null;
    cols: Array<colDef> = [];
    rows: Array<any> = [];
    showModal = false;
    selectedSecurityTestCase: SecurityTestCase | null = null;
    contractMethods: ContractMethodUI[] = [];

    constructor(
        private route: ActivatedRoute,
        private contractService: ContractService,
        private router: Router,
    ) {
        this.contractId = this.route.snapshot.paramMap.get('contractId');

        this.initDatatableColumns();
    }

    ngOnInit() {
        if (this.contractId) {
            this.contractService.getContract(this.contractId).subscribe({
                next: (contractAnalysis) => {
                    this.contractAnalysis = contractAnalysis;
                    this.createContractMethodsUI();
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
            { field: 'severity', title: 'Severity' },
            { field: 'action', title: 'Action', sort: false, headerClass: 'justify-center' },
        ];
    }

    download() {
        this.contractService.downloadContract(this.contractAnalysis?.id!);
    }

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

    goToExecuteCases() {
        this.router.navigate(['/audit/results/', this.contractId]);
    }
}
