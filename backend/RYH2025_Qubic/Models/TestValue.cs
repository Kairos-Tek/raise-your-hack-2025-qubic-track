using System.ComponentModel.DataAnnotations;

namespace RYH2025_Qubic.Models
{
    public class TestValue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string TestCaseId { get; set; }

        [Required]
        public string VariableId { get; set; }

        [Required]
        public string Value { get; set; }

        public string Description { get; set; } = string.Empty;

        // Navigation properties
        public TestCase TestCase { get; set; }
        public MethodVariable Variable { get; set; }
    }
}