using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RYH2025_Qubic.Models
{
    public class ContractAnalysis
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string ContractName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public virtual SecurityAuditResult? SecurityAudit { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<ContractMethod> Methods { get; set; } = new List<ContractMethod>();

    }
}