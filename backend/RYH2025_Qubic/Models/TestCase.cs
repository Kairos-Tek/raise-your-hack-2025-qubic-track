using System.ComponentModel.DataAnnotations;

namespace RYH2025_Qubic.Models
{
    public class TestCase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string MethodId { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string RiskLevel { get; set; } // "low", "medium", "high"

        [Required]
        public string Category { get; set; } // "boundary", "overflow", "logic", "access", "reentrancy"

        // Navigation properties
        public ContractMethod Method { get; set; }
        public ICollection<TestValue> TestValues { get; set; } = new List<TestValue>();
        public TestResult? Result { get; set; }
    }
}
