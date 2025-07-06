import { QubicTransaction } from '@qubic-lib/qubic-ts-library/dist/qubic-types/QubicTransaction';
import { ContractMethod } from './contract-method.models';
import { SecurityTestCase } from './security-test-case.model';
import { DynamicPayload } from '@qubic-lib/qubic-ts-library/dist/qubic-types/DynamicPayload';

export interface TestExecutionResult {
    testCase: SecurityTestCase;
    method: ContractMethod;
    transaction?: QubicTransaction;
    payload?: DynamicPayload;
    executionStatus: 'success' | 'failed' | 'rejected' | 'error';
    broadcastResult?: any;
    error?: string;
    actualBehavior?: string;
    securityAssessment: {
        vulnerabilityConfirmed: boolean;
        riskLevel: string;
        notes: string;
    };
}
