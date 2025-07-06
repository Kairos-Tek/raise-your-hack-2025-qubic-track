export interface SecurityTestCase {
    id: string;
    securityAuditResultId: string;
    contractMethodId: string;
    testName: string;
    methodName: string;
    targetVariable: string;
    description: string;
    vulnerabilityType: string;
    severity: string;
    expectedBehavior: string;
    actualRisk: string;
    createdAt: string;
    testInputs: { [key: string]: any };
    mitigationSteps: string[];
}
