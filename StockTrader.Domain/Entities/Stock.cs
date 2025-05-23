﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Domain.Entities
{
    public class Stock
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public virtual ICollection<UserStockHolding> userstockholdings { get; set; } = new List<UserStockHolding>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public Stock()
        {

        }
    }
}
