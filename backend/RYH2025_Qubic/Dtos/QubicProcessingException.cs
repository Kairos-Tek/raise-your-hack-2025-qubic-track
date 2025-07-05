using System;

namespace RYH2025_Qubic.Dtos
{
    public class QubicProcessingException : Exception
    {
        public QubicProcessingException(string message) : base(message) { }
        public QubicProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
