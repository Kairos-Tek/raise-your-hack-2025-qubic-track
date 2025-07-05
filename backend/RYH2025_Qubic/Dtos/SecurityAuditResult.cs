using System;
using System.Collections.Generic;

namespace RYH2025_Qubic.Dtos
{
    public class SecurityAuditResult
    {
        public string ContractName { get; set; } = string.Empty;
        public DateTime AuditDate { get; set; }
        public List<VulnerabilityFound> Vulnerabilities { get; set; } = new();
        public Dictionary<string, List<SecurityTestCase>> SecurityTests { get; set; } = new();
        public SecurityRisk OverallRisk { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}
