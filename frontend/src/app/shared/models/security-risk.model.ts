export interface SecurityRisk {
    id: string;
    securityAuditResultId: string;
    level: string;
    score: number;
    summary: string;
    createdAt: string;
    factors: string[];
}
