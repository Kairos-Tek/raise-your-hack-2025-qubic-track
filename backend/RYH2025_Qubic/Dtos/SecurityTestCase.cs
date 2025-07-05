using System.Collections.Generic;

namespace RYH2025_Qubic.Dtos
{
    public class SecurityTestCase
    {
        public string TestName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VulnerabilityType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public Dictionary<string, object> TestInputs { get; set; } = new();
        public string ExpectedBehavior { get; set; } = string.Empty;
        public string ActualRisk { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public List<string> MitigationSteps { get; set; } = new();
    }
}
