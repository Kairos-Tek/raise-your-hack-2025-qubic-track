import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ContractService } from 'src/app/service/contract.service';
import { ContractAnalysis } from 'src/app/shared/models/contract-analysis.model';

@Component({
    selector: 'app-results-dashboard',
    templateUrl: './results-dashboard.component.html',
})
export class ResultsDashboardComponent {
    contractId: string | null = null;
    contractAnalysis: ContractAnalysis | null = null;

    constructor(
        private route: ActivatedRoute,
        private contractService: ContractService,
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
}
