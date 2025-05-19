using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public decimal CashAmount { get; set; }
        public virtual ICollection<UserStockHolding> userstockholdings { get; set; } = new List<UserStockHolding>();

        public ApplicationUser() : base()
        {
            CashAmount = 100000.00m;
        }
    }
}
