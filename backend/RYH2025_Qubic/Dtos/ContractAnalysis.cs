using System.Collections.Generic;

namespace RYH2025_Qubic.Dtos
{
    public class ContractAnalysis
    {
        public string ContractName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<ContractMethod> Methods { get; set; } = new();
    }
}
