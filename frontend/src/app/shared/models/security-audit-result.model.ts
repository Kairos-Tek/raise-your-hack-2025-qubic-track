import { SecurityRisk } from './security-risk.model';
import { SecurityTestCase } from './security-test-case.model';
import { VulnerabilityFound } from './vulnerability-found';

export interface SecurityAuditResult {
    id: string;
    contractName: string;
    auditDate: string;
    recommendations: string[];
    vulnerabilities: VulnerabilityFound[];
    securityTests: SecurityTestCase[];
    overallRisk: SecurityRisk;
}
