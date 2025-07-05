using System;

namespace RYH2025_Qubic.Dtos
{
    public class ProcessingOptions
    {
        public bool GenerateCode { get; set; } = true;
        public bool GenerateInterfaces { get; set; } = true;
        public bool GenerateHelpers { get; set; } = true;
        public bool GenerateValidations { get; set; } = true;
        public bool PerformSecurityAudit { get; set; } = true;
        public bool GenerateSecurityTests { get; set; } = true;
        public int MaxConcurrentRequests { get; set; } = 3;
        public string? OutputDirectory { get; set; }
    }
}
