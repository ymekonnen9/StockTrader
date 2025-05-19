using StockTrader.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public int StockId { get; set; }
        public Stock Stock { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal PriceAtOrdered { get; set; }
        public DateTime TimeOrderedAt { get; set; }
        public OrderType OrderType { get; set; }
        public OrderStatus OrderStatus { get; set; }

        public Order()
        {
            Id = Guid.NewGuid();
            TimeOrderedAt = DateTime.UtcNow;
        }
    }
}
