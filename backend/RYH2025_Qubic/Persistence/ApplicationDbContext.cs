using Microsoft.EntityFrameworkCore;
using RYH2025_Qubic.Dtos;
using RYH2025_Qubic.Models;
using System.Text.Json;

namespace RYH2025_Qubic.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ContractAnalysis> ContractAnalyses { get; set; }
        public DbSet<ContractMethod> ContractMethods { get; set; }
        public DbSet<SecurityAuditResult> SecurityAuditResults { get; set; }
        public DbSet<VulnerabilityFound> VulnerabilitiesFound { get; set; }
        public DbSet<SecurityTestCase> SecurityTestCases { get; set; }
        public DbSet<SecurityRisk> SecurityRisks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("raiseyourhack");

            modelBuilder.Entity<ContractAnalysis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContractName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Namespace).HasMaxLength(200);
                entity.HasIndex(e => e.ContractName);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ContractMethod
            modelBuilder.Entity<ContractMethod>(entity =>
            {
                // Existing configuration
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                
                entity.Property(e => e.ProcedureIndex).IsRequired(false);
                entity.Property(e => e.PackageSize).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.IsAssetRelated).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.IsOrderBookRelated).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // JSON fields
                entity.Property(e => e.InputStructJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");
                entity.Property(e => e.OutputStructJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");
                entity.Property(e => e.InputFieldsJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("[]");
                entity.Property(e => e.OutputFieldsJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("[]");
                entity.Property(e => e.FeesJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");
                entity.Property(e => e.ValidationsJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("[]");

                // Relationships
                entity.HasOne(e => e.ContractAnalysis)
                      .WithMany(e => e.Methods)
                      .HasForeignKey(e => e.ContractAnalysisId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.SecurityTestCases)
                      .WithOne(e => e.ContractMethod)
                      .HasForeignKey(e => e.ContractMethodId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ContractAnalysisId);
                entity.HasIndex(e => e.Name);

                // NEW: Additional indexes for better performance
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.PackageSize);
                entity.HasIndex(e => e.ProcedureIndex).HasFilter("\"ProcedureIndex\" IS NOT NULL");
                entity.HasIndex(e => e.IsAssetRelated).HasFilter("\"IsAssetRelated\" = true");
                entity.HasIndex(e => e.IsOrderBookRelated).HasFilter("\"IsOrderBookRelated\" = true");
                entity.HasIndex(e => new { e.ContractAnalysisId, e.Name }).IsUnique();
                entity.HasIndex(e => new { e.Type, e.Name });

                // JSON indexes 
                entity.HasIndex(e => e.InputFieldsJson).HasMethod("gin");
                entity.HasIndex(e => e.OutputFieldsJson).HasMethod("gin");
                entity.HasIndex(e => e.FeesJson).HasMethod("gin");
                entity.HasIndex(e => e.ValidationsJson).HasMethod("gin");

            });

            // SecurityAuditResult
            modelBuilder.Entity<SecurityAuditResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContractName).HasMaxLength(200).IsRequired();

                entity.HasIndex(e => e.AuditDate);
            });

            // SecurityRisk (One-to-One with SecurityAuditResult)
            modelBuilder.Entity<SecurityRisk>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(500);

                entity.HasOne(e => e.SecurityAuditResult)
                      .WithOne(e => e.OverallRisk)
                      .HasForeignKey<SecurityRisk>(e => e.SecurityAuditResultId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // VulnerabilityFound
            modelBuilder.Entity<VulnerabilityFound>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Severity).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Location).HasMaxLength(500);
                entity.Property(e => e.Impact).HasMaxLength(1000);

                entity.HasOne(e => e.SecurityAuditResult)
                      .WithMany(e => e.Vulnerabilities)
                      .HasForeignKey(e => e.SecurityAuditResultId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Severity);
                entity.HasIndex(e => e.Type);
            });

            // SecurityTestCase
            modelBuilder.Entity<SecurityTestCase>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TestName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.MethodName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.TargetVariable).HasMaxLength(100).IsRequired();
                entity.Property(e => e.VulnerabilityType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Severity).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ExpectedBehavior).HasMaxLength(500);
                entity.Property(e => e.ActualRisk).HasMaxLength(500);

                entity.Property(e => e.ExecutionResultJson)
                               .HasColumnType("jsonb")
                               .IsRequired(false);
                entity.Property(e => e.LastExecutedAt)
                    .IsRequired(false);
                entity.Property(e => e.ExecutionStatus)
                    .HasMaxLength(50)
                    .IsRequired(false);
                entity.Property(e => e.VulnerabilityConfirmed)
                    .IsRequired(false);
                entity.Property(e => e.RiskLevel)
                    .HasMaxLength(50)
                    .IsRequired(false);

                entity.HasOne(e => e.SecurityAuditResult)
                      .WithMany(e => e.SecurityTests)
                      .HasForeignKey(e => e.SecurityAuditResultId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ContractMethod)
                      .WithMany(e => e.SecurityTestCases)
                      .HasForeignKey(e => e.ContractMethodId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.MethodName);
                entity.HasIndex(e => e.TargetVariable);
                entity.HasIndex(e => e.Severity);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}