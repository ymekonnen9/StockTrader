using StockTrader.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Application.Services
{
    public interface IPortfolioService
    {
        Task<PortfolioDto?> GetPortfolioAsync(string userId);
    }
}
