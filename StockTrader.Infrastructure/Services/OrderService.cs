using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockTrader.Application.DTOs;
using StockTrader.Application.Services;
using StockTrader.Domain.Entities;
using StockTrader.Domain.Enums;
using StockTrader.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly UserManager<ApplicationUser> _usermanager;
        private readonly ApplicationDbContext _context;

        public OrderService(UserManager<ApplicationUser> usermanager, ApplicationDbContext context)
        {
            _usermanager = usermanager;
            _context = context;
        }

        public async Task<OrderPlacementResultDto> PlaceBuyOrderAsync(string userId, BuyOrderRequestDto requestDto)
        {
            var user = await _usermanager.FindByIdAsync(userId);

            if(user == null)
            {
                return new OrderPlacementResultDto
                {
                    Message = "The user donesn't exist",
                    Success = false
                };
            }


            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol.ToUpper() == requestDto.StockSymbol.ToUpper());

            if(stock == null)
            {
                return new OrderPlacementResultDto
                {
                    Message = "The stock symbol is wrong",
                    Success = false
                };
            }

            var currentStockPrice = stock.CurrentPrice;
            var totalCost = currentStockPrice * requestDto.Quantity;

            if(totalCost > user.CashAmount)
            {
                return new OrderPlacementResultDto
                {
                    Message = "You have insufficent funds",
                    Success = false
                };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    ApplicationUserId = user.Id,
                    StockId = stock.Id,
                    OrderType = OrderType.Buy,
                    Quantity = requestDto.Quantity,
                    PriceAtOrdered = currentStockPrice,
                    TimeOrderedAt = DateTime.UtcNow,
                    OrderStatus = OrderStatus.Filled
                };

                await _context.Orders.AddAsync(order);
                user.CashAmount -= totalCost;

                var userstockholding = await _context.UserStockHoldings.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id && s.StockId == stock.Id);
                if(userstockholding != null)
                {
                    var oldTotalValue = userstockholding.Quantity * userstockholding.AveragePurchasePrice;
                    var newTotalValue = userstockholding.Quantity * currentStockPrice;

                    userstockholding.Quantity += requestDto.Quantity;
                    userstockholding.AveragePurchasePrice = (oldTotalValue + newTotalValue) / userstockholding.Quantity;
                } else
                {
                    var newstockholding = new UserStockHolding
                    {
                        ApplicationUserId = user.Id,
                        StockId = stock.Id,
                        Quantity = requestDto.Quantity,
                        AveragePurchasePrice = stock.CurrentPrice,
                    };

                    _context.UserStockHoldings.Add(newstockholding);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OrderPlacementResultDto
                {
                    Success = true,
                    Message = $"Successfully bought {requestDto.Quantity} shares of {stock.Symbol}.",
                    OrderId = order.Id,
                    NewCashBalance = user.CashAmount,
                    StockSymbol = stock.Symbol,
                    QuantityFilled = requestDto.Quantity,
                    PriceFilled = currentStockPrice
                };
            

            }catch(Exception e)
            {
                return new OrderPlacementResultDto
                {
                    Message = $"There was something wrong{e.Message}",
                    Success = false
                };
            }
        }



        public async Task<OrderPlacementResultDto> PlaceSellOrderAsync(string userId, SellOrderRequestDto requestDto)
        { 

            var user = await _usermanager.FindByIdAsync(userId);
            if(user == null)
            {
                return new OrderPlacementResultDto
                {
                    Message = "User identification failed",
                    Success = false
                };
            }
            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Symbol.ToUpper() == user.Id.ToUpper());

            if(stock == null)
            {
                return new OrderPlacementResultDto
                {
                    Message = "The stock symbol is not correct",
                    Success = false
                };
            }

            var stockholding = await _context.UserStockHoldings.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id && s.StockId == stock.Id);
            if(stockholding == null || stockholding.Quantity < stockholding.Quantity)
            {
                return new OrderPlacementResultDto
                {
                    Message = "You do not have enough stock shares",
                    Success = false,
                };
            }

            var currentStockPrice = stock.CurrentPrice;
            var totalProceeds = currentStockPrice * requestDto.Quantity;
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    ApplicationUserId = user.Id,
                    StockId = stock.Id,
                    Quantity = requestDto.Quantity,
                    OrderType = OrderType.Sell,
                    PriceAtOrdered = stock.CurrentPrice,
                    OrderStatus = OrderStatus.Filled,
                    TimeOrderedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);

                user.CashAmount += totalProceeds;

                stockholding.Quantity -= requestDto.Quantity;

                if(stockholding.Quantity == 0)
                {
                    _context.UserStockHoldings.Remove(stockholding);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OrderPlacementResultDto
                {
                    Success = true,
                    Message = $"Successfully sold{requestDto.Quantity} stocks",
                    OrderId = order.Id,
                    NewCashBalance = user.CashAmount,
                    StockSymbol = stock.Symbol,
                    QuantityFilled = requestDto.Quantity,
                    PriceFilled = stock.CurrentPrice

                };

            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                return new OrderPlacementResultDto
                {
                    Message = $"There was a concurrency error on your order{ex.Message}",
                    Success = false
                };

            }
            catch(Exception e)
            {
                return new OrderPlacementResultDto
                {
                    Message = $"There was something wrong{e.Message}",
                    Success = false
                };
            }
        }
    }
}
