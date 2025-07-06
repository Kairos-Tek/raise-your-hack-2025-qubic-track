import { ContractMethod } from './contract-method.models';
import { SecurityAuditResult } from './security-audit-result.model';

export interface ContractAnalysis {
    id: string;
    contractName: string;
    namespace: string;
    securityAudit?: SecurityAuditResult;
    createdAt: string;
    methods: ContractMethod[];
}
