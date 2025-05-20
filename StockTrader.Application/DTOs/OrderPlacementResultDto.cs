using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Application.DTOs
{
    public class OrderPlacementResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? OrderId { get; set; }
        public string? StockSymbol { get; set; } = string.Empty;
        public decimal? NewCashBalance { get; set; }
        public int? QuantityFilled { get; set; }
        public decimal? PriceFilled { get; set; }

    }
}
