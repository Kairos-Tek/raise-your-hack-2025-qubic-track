using System.Text.Json.Serialization;

namespace RYH2025_Qubic.Dtos
{
    internal class GroqMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
