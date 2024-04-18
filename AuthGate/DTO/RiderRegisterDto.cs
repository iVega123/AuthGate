using System.ComponentModel.DataAnnotations;

namespace AuthGate.DTO
{
    public class RiderRegisterDto : RegisterDto
    {
        [Required]
        [StringLength(20)]
        [RegularExpression(
    @"([0-9]{2}[\.]?[0-9]{3}[\.]?[0-9]{3}[\/]?[0-9]{4}[-]?[0-9]{2})",
    ErrorMessage = "O CNPJ deve estar no formato XX.XXX.XXX/XXXX-XX")]
        public required string CNPJ { get; set; }
        
        public DateTime DataNascimento { get; set; }
        
        [StringLength(11)]
        [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "O número da CNH deve conter 11 dígitos")]
        public required string NumeroCNH { get; set; }
        
        public required string TipoCNH { get; set; }
        
        public required string ImagemCNH { get; set; }
    }
}
