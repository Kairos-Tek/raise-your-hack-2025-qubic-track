using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace RYH2025_Qubic.Models
{
    public class ContractMethod
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Key
        public Guid ContractAnalysisId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // FUNCTION or PROCEDURE
        public int? ProcedureIndex { get; set; }

        // Stored as JSON strings in database
        [Column(TypeName = "jsonb")]
        public string InputStructJson { get; set; } = "{}";

        [Column(TypeName = "jsonb")]
        public string OutputStructJson { get; set; } = "{}";

        [Column(TypeName = "jsonb")]
        public string FeesJson { get; set; } = "{}";

        [Column(TypeName = "jsonb")]
        public string ValidationsJson { get; set; } = "[]";

        public string Description { get; set; } = string.Empty;
        public bool IsAssetRelated { get; set; }
        public bool IsOrderBookRelated { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ContractAnalysisId")]
        public virtual ContractAnalysis ContractAnalysis { get; set; } = null!;

        public virtual ICollection<SecurityTestCase> SecurityTestCases { get; set; } = new List<SecurityTestCase>();

        // Helper properties (not mapped to DB)
        [NotMapped]
        [JsonPropertyName("inputStruct")]
        public JsonElement InputStructRaw
        {
            get => JsonSerializer.Deserialize<JsonElement>(InputStructJson);
            set => InputStructJson = value.GetRawText();
        }

        [NotMapped]
        [JsonPropertyName("outputStruct")]
        public JsonElement OutputStructRaw
        {
            get => JsonSerializer.Deserialize<JsonElement>(OutputStructJson);
            set => OutputStructJson = value.GetRawText();
        }

        [NotMapped]
        public Dictionary<string, object> Fees
        {
            get => JsonSerializer.Deserialize<Dictionary<string, object>>(FeesJson) ?? new();
            set => FeesJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public List<string> Validations
        {
            get => JsonSerializer.Deserialize<List<string>>(ValidationsJson) ?? new();
            set => ValidationsJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        [JsonIgnore]
        public Dictionary<string, string> InputStruct
        {
            get
            {
                if (InputStructRaw.ValueKind == JsonValueKind.Object)
                {
                    var result = new Dictionary<string, string>();
                    foreach (var property in InputStructRaw.EnumerateObject())
                    {
                        result[property.Name] = property.Value.GetString() ?? "unknown";
                    }
                    return result;
                }
                return new Dictionary<string, string>();
            }
        }

        [NotMapped]
        [JsonIgnore]
        public Dictionary<string, string> OutputStruct
        {
            get
            {
                if (OutputStructRaw.ValueKind == JsonValueKind.Object)
                {
                    var result = new Dictionary<string, string>();
                    foreach (var property in OutputStructRaw.EnumerateObject())
                    {
                        result[property.Name] = property.Value.GetString() ?? "unknown";
                    }
                    return result;
                }
                return new Dictionary<string, string>();
            }
        }
    }
}