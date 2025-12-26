using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; } // nullable int для форми

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        public int Quantity { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock cannot be negative.")]
        public int? MinimumStock { get; set; } = 0;

        public Category? Category { get; set; }

        [NotMapped]
        public bool IsLowStock => MinimumStock > 0 && Quantity < MinimumStock;
    }
}
