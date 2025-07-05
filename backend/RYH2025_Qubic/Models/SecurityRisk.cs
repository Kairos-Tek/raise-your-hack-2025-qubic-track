using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RYH2025_Qubic.Models
{
    public class SecurityRisk
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Key (One-to-One)
        public Guid SecurityAuditResultId { get; set; }

        public string Level { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Summary { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Stored as JSON
        [Column(TypeName = "jsonb")]
        public string FactorsJson { get; set; } = "[]";

        // Navigation property
        [ForeignKey("SecurityAuditResultId")]
        public virtual SecurityAuditResult SecurityAuditResult { get; set; } = null!;

        // Helper property
        [NotMapped]
        public List<string> Factors
        {
            get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(FactorsJson) ?? new();
            set => FactorsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}