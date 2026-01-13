using System.ComponentModel.DataAnnotations;

namespace WarehouseApp.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }

        [Required]
        public int? ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required]
        [RegularExpression("IN|OUT", ErrorMessage = "Transaction type must be IN or OUT")]
        public string TransactionType { get; set; } = "IN";

        public DateTime Date { get; set; } = DateTime.Now;

        public string? Comment { get; set; }

        public Product? Product { get; set; }
    }
}
