using System.Collections.Generic;

namespace RYH2025_Qubic.Dtos
{
    public class SecurityRisk
    {
        public string Level { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
    }
}
