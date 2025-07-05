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
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasOne(e => e.ContractAnalysis)
                      .WithMany(e => e.Methods)
                      .HasForeignKey(e => e.ContractAnalysisId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ContractAnalysisId);
                entity.HasIndex(e => e.Name);
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