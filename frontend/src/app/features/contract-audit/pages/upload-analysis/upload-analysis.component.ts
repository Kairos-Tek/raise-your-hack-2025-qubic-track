import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ContractService } from 'src/app/service/contract.service';
import { Contract } from 'src/app/shared/models/contract.models';
import { AnalyzeContractRequest } from 'src/app/shared/models/requests.models';
import { Router } from '@angular/router';
import { ContractAnalysis } from 'src/app/shared/models/contract-analysis.model';
import { ContractMethod } from 'src/app/shared/models/contract-method.models';

@Component({
    selector: 'app-upload-analysis',
    templateUrl: './upload-analysis.component.html',
})
export class UploadAnalysisComponent {
    form: FormGroup;
    uploaded = false;
    fileName: string | null = null;
    fileExtension: string | null = null;
    isLoading = false;
    contractData: Contract | null = null;
    contractAnalysis: ContractAnalysis | null = null;

    constructor(
        private contractService: ContractService,
        private fb: FormBuilder,
        private router: Router,
    ) {
        this.form = this.fb.group({
            nameAudit: ['', Validators.required],
            file: [null, Validators.required],
        });

        this.contractService.analyzeMockContract().subscribe({
            next: (contract) => {
                this.contractData = contract;
            },
        });
        this.contractService.getContract('817e23e8-2900-4f61-9bdc-e570397a34b5').subscribe({
            next: (contract) => {
                debugger;
                this.contractAnalysis = contract;
            },
        });
    }

    onFileChange(event: Event) {
        const input = event.target as HTMLInputElement;
        const file = input.files && input.files[0];
        this.uploaded = !!file;
        if (file) {
            this.fileName = file.name;
            const parts = file.name.split('.');
            this.fileExtension = parts.length > 1 ? parts.pop()! : null;

            this.form.patchValue({ file });
            this.form.get('file')!.updateValueAndValidity();
        } else {
            this.fileName = null;
            this.fileExtension = null;

            this.form.patchValue({ file: null });
        }
    }

    onSubmit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const file = this.form.value.file;
        if (!file) {
            console.error('No file selected');
            return;
        }

        this.isLoading = true;
        const request: AnalyzeContractRequest = { file };
        this.contractService.analyzeContract(request).subscribe({
            next: (contractAnalysis) => {
                console.log('Contract analyzed successfully:', contractAnalysis);
                this.isLoading = false;
                debugger;
                this.contractAnalysis = contractAnalysis;
            },
            error: (error) => {
                console.error('Error analyzing contract:', error);
                this.isLoading = false;
            },
        });
    }

    generateTestCases() {
        if (this.contractAnalysis) {
            this.router.navigate(['/audit/test', this.contractAnalysis.id]);
        }
    }

    getCardSubtitle(method: ContractMethod): string {
        return `${method.name}(${method.inputFields.map((f) => `${f.name} ${f.qubicType}`).join(', ')}) : ${method.outputFields.map((f) => `${f.name} ${f.qubicType}`).join(', ')}`;
    }
}
