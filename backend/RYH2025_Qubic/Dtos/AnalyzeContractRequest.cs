using System.ComponentModel.DataAnnotations;
namespace RYH2025_Qubic.Dtos
{
    public class AnalyzeContractRequest
    {
        [Required(ErrorMessage = "El archivo es requerido")]
        public IFormFile File { get; set; } = null!;
    }
}
