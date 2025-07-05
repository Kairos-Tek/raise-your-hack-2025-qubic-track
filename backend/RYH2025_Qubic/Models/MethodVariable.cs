using System.ComponentModel.DataAnnotations;

namespace RYH2025_Qubic.Models
{
    public class MethodVariable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string MethodId { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string Type { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public int Position { get; set; }
        
        // Navigation properties
        public ContractMethod Method { get; set; }
        public ICollection<TestValue> TestValues { get; set; } = new List<TestValue>();
    }
}
