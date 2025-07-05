using System;

namespace RYH2025_Qubic.Dtos
{
    public class GroqApiException : Exception
    {
        public GroqApiException(string message) : base(message) { }
        public GroqApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}
