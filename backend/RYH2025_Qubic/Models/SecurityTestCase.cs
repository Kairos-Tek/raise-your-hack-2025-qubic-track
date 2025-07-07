using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RYH2025_Qubic.Models
{
    public class SecurityTestCase
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Keys
        public Guid SecurityAuditResultId { get; set; }
        public Guid ContractMethodId { get; set; }

        public string TestName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string TargetVariable { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VulnerabilityType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string ExpectedBehavior { get; set; } = string.Empty;
        public string ActualRisk { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Column(TypeName = "jsonb")]
        public string TestInputsJson { get; set; } = "{}";

        [Column(TypeName = "jsonb")]
        public string MitigationStepsJson { get; set; } = "[]";
        [Column(TypeName = "jsonb")]
        public string? ExecutionResultJson { get; set; }
        public DateTime? LastExecutedAt { get; set; }
        public string? ExecutionStatus { get; set; }
        public bool? VulnerabilityConfirmed { get; set; }
        public string? RiskLevel { get; set; }

        [JsonIgnore]
        [ForeignKey("SecurityAuditResultId")]
        public virtual SecurityAuditResult SecurityAuditResult { get; set; } = null!;

        [JsonIgnore]
        [ForeignKey("ContractMethodId")]
        public virtual ContractMethod ContractMethod { get; set; } = null!;

        [NotMapped]
        public Dictionary<string, object> TestInputs
        {
            get => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(TestInputsJson) ?? new();
            set => TestInputsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public List<string> MitigationSteps
        {
            get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(MitigationStepsJson) ?? new();
            set => MitigationStepsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}