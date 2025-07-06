import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ContractService } from 'src/app/service/contract.service';
import { SecurityTestExecutorService } from 'src/app/service/security-test-executor.service';
import { ContractAnalysis } from 'src/app/shared/models/contract-analysis.model';
import { ContractMethod } from 'src/app/shared/models/contract-method.models';
import { Contract } from 'src/app/shared/models/contract.models';
import { SecurityTestCase } from 'src/app/shared/models/security-test-case.model';
import { TestCase } from 'src/app/shared/models/test-case.models';
import { TestExecutionConfig } from 'src/app/shared/models/test-execution-config.model';

@Component({
    selector: 'app-test-management',
    templateUrl: './test-management.component.html',
})
export class TestManagementComponent {
    contractId: string | null = null;
    contractAnalysis: ContractAnalysis | null = null;

    constructor(
        private route: ActivatedRoute,
        private contractService: ContractService,
        private securityTestExecutorService: SecurityTestExecutorService,
    ) {
        this.contractId = this.route.snapshot.paramMap.get('contractId');
    }

    ngOnInit() {
        if (this.contractId) {
            this.contractService.getContract(this.contractId).subscribe({
                next: (contractAnalysis) => {
                    this.contractAnalysis = contractAnalysis;
                },
                error: (error) => {
                    console.error('Error generating test cases:', error);
                },
            });
        }
    }

    executeTestCase(testCase: SecurityTestCase, method: ContractMethod) {
        debugger;
        var config = <TestExecutionConfig>{
            qubicApiUrl: 'http://67.222.157.62:8000',
            contractAddress: 'MAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAWLWD',
            testIdentity: 'HYJXEZSECWSKODJALRCKSLKVYUEBMAHDODYZUJIIYDPAGFKLMOTHTJXEBEWM',
            testSeed: 'ukzbkszgzpipmxrrqcxcppumxoxzerrvbjgthinzodrlyblkedutmsy',
            tickOffset: 20,
            delayBetweenTests: 200,
        };

        this.securityTestExecutorService.executeSecurityTest(testCase, method, config).subscribe({
            next: (result) => {
                debugger;

                console.log('Test executed successfully:', result);
            },
            error: (error) => {
                console.error('Error executing test:', error);
            },
        });
    }

    getCardSubtitle(method: ContractMethod): string {
        return `${method.name}(${method.inputFields.map((f) => `${f.name} ${f.qubicType}`).join(', ')}) : ${method.outputFields.map((f) => `${f.name} ${f.qubicType}`).join(', ')}`;
    }

    getSecurityTestCases(method: ContractMethod): SecurityTestCase[] {
        return this.contractAnalysis?.securityAudit?.securityTests.filter((testCase) => testCase.methodName === method.name) || [];
    }
}
