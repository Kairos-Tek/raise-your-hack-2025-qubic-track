using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RYH2025_Qubic.Models
{
    public class SecurityAuditResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Key
        public Guid CompleteAnalysisResultId { get; set; }

        public string ContractName { get; set; } = string.Empty;
        public DateTime AuditDate { get; set; } = DateTime.UtcNow;

        // Stored as JSON
        [Column(TypeName = "jsonb")]
        public string RecommendationsJson { get; set; } = "[]";

        public virtual ICollection<VulnerabilityFound> Vulnerabilities { get; set; } = new List<VulnerabilityFound>();
        public virtual ICollection<SecurityTestCase> SecurityTests { get; set; } = new List<SecurityTestCase>();
        public virtual SecurityRisk OverallRisk { get; set; } = null!;

        // Helper property
        [NotMapped]
        public List<string> Recommendations
        {
            get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(RecommendationsJson) ?? new();
            set => RecommendationsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}