using Microsoft.AspNetCore.Identity;
using StockTrader.Application.DTOs;
using StockTrader.Application.Services;
using StockTrader.Domain.Entities;
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

        public PortfolioService(UserManager<ApplicationUser> usermanager)
        {
            _usermanager = usermanager;
        }

        public async Task<PortfolioDto?> GetPortfolioAsync(string userId)
        {
            var user = await _usermanager.FindByIdAsync(userId);

            if(user == null)
            {
                return null;
            }

            return new PortfolioDto
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                CashAmount = user.CashAmount
            };
        }


    }
}
