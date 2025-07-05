using System.ComponentModel.DataAnnotations;

namespace RYH2025_Qubic.Models
{

    public class Contract
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string FileName { get; set; }

        [Required]
        public string ContractCode { get; set; }

        public string Summary { get; set; } = string.Empty;

        public List<string> Vulnerabilities { get; set; } = new();

        public string? FakeContractAddress { get; set; }

        public string? FakeTransactionHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ContractMethod> Methods { get; set; } = new List<ContractMethod>();
    }
}
