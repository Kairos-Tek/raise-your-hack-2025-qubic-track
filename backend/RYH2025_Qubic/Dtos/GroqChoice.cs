using System.Text.Json.Serialization;

namespace RYH2025_Qubic.Dtos
{
    internal class GroqChoice
    {
        [JsonPropertyName("message")]
        public GroqMessage Message { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = string.Empty;
    }
}
