using System.ComponentModel.DataAnnotations;

namespace RYH2025_Qubic.Models
{
    public class TestResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string TestCaseId { get; set; }

        [Required]
        public string Status { get; set; } // "pending", "running", "success", "failed", "error"

        public int ExecutionTime { get; set; }

        public int? GasUsed { get; set; }

        public string? Error { get; set; }

        public string? ReturnValue { get; set; }

        public string? FakeTransactionHash { get; set; }

        public DateTime? ExecutedAt { get; set; }

        // Navigation properties
        public TestCase TestCase { get; set; }
    }
}
