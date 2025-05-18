using Microsoft.EntityFrameworkCore;
using StockTrader.Application.DTOs;
using StockTrader.Application.Services;
using StockTrader.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Infrastructure.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;

        public StockService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockDto>> GetAllStocksAsync()
        {
            return await _context.Stocks.Select(stock => new StockDto
            {
                Id = stock.Id,
                Symbol = stock.Symbol,
                CompanyName = stock.CompanyName,
                CurrentPrice = stock.CurrentPrice
            }
            ).ToListAsync();
        }

        public async Task<StockDto?> GetStockBySymbol(string symbol)
        {
            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol.ToUpper() == symbol.ToUpper());

            if(stock == null)
            {
                return null;
            }

            return new StockDto
            {
                Id = stock.Id,
                Symbol = stock.Symbol,
                CompanyName = stock.CompanyName,
                CurrentPrice = stock.CurrentPrice
            };
        }
    }
}
