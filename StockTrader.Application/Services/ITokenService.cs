using StockTrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Application.Services
{
    public interface ITokenService
    {
        Task<String> GenerateTokenAsync(ApplicationUser user);
    }
}
