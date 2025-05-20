using StockTrader.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrader.Application.Services
{
    public interface IOrderService
    {
        Task<OrderPlacementResultDto> PlaceBuyOrderAsync(string userId, BuyOrderRequestDto orderRequest);
        Task<OrderPlacementResultDto> PlaceSellOrderAsync(string userId, SellOrderRequestDto orderRequest);
    }
}
