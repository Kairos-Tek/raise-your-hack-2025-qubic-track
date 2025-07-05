// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using RYH2025_Qubic.Models;
using System.Text.Json;
using RYH2025_Qubic.Models;

namespace ContractAuditSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractMethod> ContractMethods { get; set; }
        public DbSet<MethodVariable> MethodVariables { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<TestValue> TestValues { get; set; }
        public DbSet<TestResult> TestResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Contract Configuration
            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContractCode).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(2000);
                entity.Property(e => e.FakeContractAddress).HasMaxLength(42);
                entity.Property(e => e.FakeTransactionHash).HasMaxLength(66);

                // Convert List<string> to JSON
                entity.Property(e => e.Vulnerabilities)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>())
                    .HasColumnType("text");

                entity.HasMany(e => e.Methods)
                    .WithOne(e => e.Contract)
                    .HasForeignKey(e => e.ContractId)
                    .OnDelete(DeleteBehavior.Cascade);

            });

            // ContractMethod Configuration
            modelBuilder.Entity<ContractMethod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContractId).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Signature).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.HasMany(e => e.Variables)
                    .WithOne(e => e.Method)
                    .HasForeignKey(e => e.MethodId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.TestCases)
                    .WithOne(e => e.Method)
                    .HasForeignKey(e => e.MethodId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MethodVariable Configuration
            modelBuilder.Entity<MethodVariable>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MethodId).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasMany(e => e.TestValues)
                    .WithOne(e => e.Variable)
                    .HasForeignKey(e => e.VariableId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TestCase Configuration
            modelBuilder.Entity<TestCase>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MethodId).IsRequired();
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.RiskLevel).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(20);

                entity.HasMany(e => e.TestValues)
                    .WithOne(e => e.TestCase)
                    .HasForeignKey(e => e.TestCaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Result)
                    .WithOne(e => e.TestCase)
                    .HasForeignKey<TestResult>(e => e.TestCaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TestValue Configuration
            modelBuilder.Entity<TestValue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TestCaseId).IsRequired();
                entity.Property(e => e.VariableId).IsRequired();
                entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Description).HasMaxLength(1000);
            });

            // TestResult Configuration
            modelBuilder.Entity<TestResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TestCaseId).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Error).HasMaxLength(2000);
                entity.Property(e => e.ReturnValue).HasMaxLength(1000);
                entity.Property(e => e.FakeTransactionHash).HasMaxLength(66);
            });
          
        }
    }
}