namespace RYH2025_Qubic.Dtos
{
    public class SaveExecutionResultRequest
    {
        public Guid TestCaseId { get; set; }
        public dynamic ExecutionResult { get; set; } // Usar dynamic para flexibilidad
    }
}
