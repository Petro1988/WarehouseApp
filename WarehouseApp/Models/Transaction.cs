using System.ComponentModel.DataAnnotations;

namespace WarehouseApp.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }

        [Required]
        public int? ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Menge muss größer als 0 sein.")]
        public int Quantity { get; set; }

        [Required]
        [RegularExpression("IN|OUT", ErrorMessage = "Bewegungstyp ist erforderlich.")]
        public string TransactionType { get; set; } = "OUT";

        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Kommentar ist erforderlich.")]
        [StringLength(500, ErrorMessage = "Kommentar darf maximal 500 Zeichen haben.")]
        public string? Comment { get; set; }

        public string CreatedBy { get; set; } = null!;

        public string? LastModifiedBy { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        public Product? Product { get; set; }
    }
}
