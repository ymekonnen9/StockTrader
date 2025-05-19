using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Domain.Entities
{
    public class UserStockHolding
    {
        public string ApplicationUserId { get; set; } = string.Empty;
        public int StockId { get; set; }

        public ApplicationUser applicationUser { get; set; } = null!;
        public Stock stock { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal AveragePrice { get; set; }
    }
}
