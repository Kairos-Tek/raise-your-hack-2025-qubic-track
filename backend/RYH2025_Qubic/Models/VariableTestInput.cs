using System.Collections.Generic;

namespace RYH2025_Qubic.Models
{
    public class VariableTestInput
    {
        public string VariableName { get; set; } = string.Empty;
        public string VariableType { get; set; } = string.Empty;
        public object MaliciousValue { get; set; } = new();
        public string AttackReason { get; set; } = string.Empty;
    }
}
