using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Application.DTOs
{
    public class BuyOrderRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
        
        [Required]
        [StringLength(10, ErrorMessage = "Stock symbol cannot exceed 10 characters.")]
        public string StockSymbol { get; set; } = string.Empty;
    }
}
