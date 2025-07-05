import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ContractService } from 'src/app/service/contract.service';
import { Contract } from 'src/app/shared/models/contract.models';

@Component({
  selector: 'app-test-management',
  templateUrl: './test-management.component.html',
})
export class TestManagementComponent {
  contractId: string | null = null;
  contractData: Contract | null = null;

  constructor(private route: ActivatedRoute, private contractService: ContractService) {
    this.contractId = this.route.snapshot.paramMap.get('contractId');
  }

  ngOnInit() {
    if (this.contractId) {
      this.contractService.generateTestCases(this.contractId).subscribe({
        next: (contract) => {
          this.contractData = contract;
          console.log('Test cases generated successfully:', this.contractData);
        },
        error: (error) => {
          console.error('Error generating test cases:', error);
        }
      });
    }
  }
}
