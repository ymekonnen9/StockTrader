using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Application.DTOs
{
    public class PortfolioDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal CashAmount { get; set; }
        public List<StockHoldingsDto> StockHoldings { get; set; } = new List<StockHoldingsDto>();
        public decimal TotalPortfolioValue { get; set; }

    }
}
