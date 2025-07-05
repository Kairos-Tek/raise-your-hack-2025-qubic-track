using System.Text.Json.Serialization;
using System.Text.Json;

namespace RYH2025_Qubic.Dtos
{
    public class ContractMethod
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // FUNCTION or PROCEDURE
        public int? ProcedureIndex { get; set; }

        // Use JsonElement to handle dynamic JSON structure
        [JsonPropertyName("inputStruct")]
        public JsonElement InputStructRaw { get; set; }

        [JsonPropertyName("outputStruct")]
        public JsonElement OutputStructRaw { get; set; }

        public Dictionary<string, object> Fees { get; set; } = new();
        public List<string> Validations { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public bool IsAssetRelated { get; set; }
        public bool IsOrderBookRelated { get; set; }

        // Helper properties to access the structs as Dictionary
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