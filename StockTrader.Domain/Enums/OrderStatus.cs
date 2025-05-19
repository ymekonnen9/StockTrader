using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Domain.Enums
{
    public enum OrderStatus
    {
        Pending,
        Filled,
        PartiallyFilled,
        Canceled,
        Failed
    }
}
