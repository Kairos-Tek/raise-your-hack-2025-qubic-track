using System;
using System.Collections.Generic;

namespace RYH2025_Qubic.Dtos
{
    public class CompleteAnalysisResult
    {
        public DateTime ProcessedAt { get; set; }
        public ProcessingOptions Options { get; set; } = new();
        public ContractAnalysis ContractAnalysis { get; set; } = new();
        public Dictionary<string, GeneratedCodeFiles> GeneratedCode { get; set; } = new();
        public SecurityAuditResult? SecurityAudit { get; set; }
    }
}
