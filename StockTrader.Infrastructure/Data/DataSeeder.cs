using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockTrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Infrastructure.Data
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _usermanager;
        private readonly RoleManager<IdentityRole> _rolemanager;

        public DataSeeder(ApplicationDbContext context, UserManager<ApplicationUser> usermanager, RoleManager<IdentityRole> rolemanager)
        {
            _context = context;
            _usermanager = usermanager;
            _rolemanager = rolemanager;
        }

        public async Task SeedAsync()
        {
            await SeedRoleAsync();
            await SeedAdminAsync();
            await SeedStockAsync();
        }

        private async Task SeedStockAsync()
        {
            if(!await _context.Stocks.AnyAsync())
            {
                var stocks = new List<Stock>
                {
                    new Stock { Symbol = "MSFT", CompanyName = "Microsoft Corp.", CurrentPrice = 425.50m },
                    new Stock { Symbol = "AAPL", CompanyName = "Apple Inc.", CurrentPrice = 170.25m },
                    new Stock { Symbol = "GOOGL", CompanyName = "Alphabet Inc.", CurrentPrice = 140.75m },
                    new Stock { Symbol = "AMZN", CompanyName = "Amazon.com Inc.", CurrentPrice = 185.00m },
                    new Stock { Symbol = "TSLA", CompanyName = "Tesla, Inc.", CurrentPrice = 175.60m }
                };

                await _context.Stocks.AddRangeAsync(stocks);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedAdminAsync()
        {
            var adminEmail = "Yared@Admin.com";
            var adminUserName = "YaredTheAdmin";

            if (await _usermanager.FindByEmailAsync(adminEmail) == null)
            {
                ApplicationUser adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    CashAmount = 1000000.00m

                };

                IdentityResult result = await _usermanager.CreateAsync(adminUser, "This!sMy9assword");
                if (result.Succeeded)
                {
                    await _usermanager.AddToRoleAsync(adminUser, "admin");
                } else
                {
                    Console.WriteLine(result.Errors.Select(e => e.Description)); 
                }
            }
        }

        private async Task SeedRoleAsync()
        {
            String[] roles = { "admin", "user" };

            foreach(String role in roles){
                if(!await _rolemanager.RoleExistsAsync(role))
                {
                    await _rolemanager.CreateAsync(new IdentityRole(role));
                } else
                {
                    Console.WriteLine("role already exists");
                }
            }
        }



    }
}
