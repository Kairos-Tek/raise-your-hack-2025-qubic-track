using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RYH2025_Qubic.Dtos
{
    internal class GroqResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public GroqUsage? Usage { get; set; }
    }
}
