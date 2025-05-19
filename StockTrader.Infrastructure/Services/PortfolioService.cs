using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockTrader.Application.DTOs;
using StockTrader.Application.Services;
using StockTrader.Domain.Entities;
using StockTrader.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Infrastructure.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly UserManager<ApplicationUser> _usermanager;
        private readonly ApplicationDbContext _context;

        public PortfolioService(UserManager<ApplicationUser> usermanager, ApplicationDbContext context)
        {
            _usermanager = usermanager;
            _context = context;
        }

        public async Task<PortfolioDto?> GetPortfolioAsync(string userId)
        {
            var user = await _usermanager.FindByIdAsync(userId);

            if(user == null)
            {
                return null;
            }

            var stockholdings = await _context.UserStockHoldings.Where(ush => ush.ApplicationUserId == user.Id).Include(ush => ush.stock).ToListAsync();
            var StockHoldingsDto = new List<StockHoldingsDto>();
            decimal totalHoldingsCurrentValue = 0;

            foreach (var holding in stockholdings)
            {
                if (holding == null) continue;

                var totalPurchaseValue = holding.Quantity * holding.AveragePurchasePrice;
                var totalCurrentValue = holding.Quantity * holding.stock.CurrentPrice;
                var gainLoss = totalCurrentValue - totalPurchaseValue;
                var gainLossPercentage = (totalPurchaseValue == 0) ? 0 : (gainLoss / totalPurchaseValue) * 100;

                StockHoldingsDto.Add(new StockHoldingsDto
                {
                    StockId = holding.StockId,
                    StockSymbol = holding.stock.Symbol,
                    CompanyName = holding.stock.CompanyName,
                    Quantity = holding.Quantity,
                    AveragePurachasePrice = holding.AveragePurchasePrice,
                    CurrentPrice = holding.stock.CurrentPrice,
                    TotalPurchaseValue = totalPurchaseValue,
                    TotalCurrentValue = totalCurrentValue,
                    GainLoss = gainLoss,
                    GainLossPercentage = gainLossPercentage
                });

                totalHoldingsCurrentValue += totalCurrentValue;

            }


            return new PortfolioDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CashAmount = user.CashAmount,
                StockHoldings = StockHoldingsDto,
                TotalPortfolioValue = user.CashAmount + totalHoldingsCurrentValue
            };
        }


    }
}
