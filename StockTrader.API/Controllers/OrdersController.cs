using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockTrader.Application.DTOs;
using StockTrader.Infrastructure.Services;
using System.Security.Claims;

namespace StockTrader.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderservice;

        public OrdersController(OrderService orderservice)
        {
            _orderservice = orderservice;
        }

        [HttpPost("buy")]
        public async Task<ActionResult<OrderPlacementResultDto>> PlaceBuyOrder([FromBody] BuyOrderRequestDto buyOrderRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { Message = "User Identification failed" });
            }

            var result = await _orderservice.PlaceBuyOrderAsync(userId, buyOrderRequest);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
    }
}
