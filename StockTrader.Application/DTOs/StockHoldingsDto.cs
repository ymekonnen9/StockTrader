using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Application.DTOs
{
    public class StockHoldingsDto
    {
        public int StockId { get; set; }
        public string StockSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal AveragePurachasePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalCurrentValue { get; set; }

        public decimal GainLoss { get; set; }
        public decimal GainLossPercentage { get; set; }

    }
}
