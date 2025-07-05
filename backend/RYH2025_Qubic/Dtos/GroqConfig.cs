namespace RYH2025_Qubic.Dtos
{
    public class GroqConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "llama-3.1-70b-versatile";
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.1;
        public int TimeoutSeconds { get; set; } = 60;
    }
}
