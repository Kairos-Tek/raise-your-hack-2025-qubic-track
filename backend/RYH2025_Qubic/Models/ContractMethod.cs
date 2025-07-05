using System.ComponentModel.DataAnnotations;

namespace RYH2025_Qubic.Models
{
    public class ContractMethod
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ContractId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Signature { get; set; }

        public string Description { get; set; } = string.Empty;

        // Navigation properties
        public Contract Contract { get; set; }
        public ICollection<MethodVariable> Variables { get; set; } = new List<MethodVariable>();
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    }
}
