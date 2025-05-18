using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockTrader.Application.DTOs;
using StockTrader.Application.Services;

namespace StockTrader.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockservice;

        public StockController(IStockService stockservice)
        {
            _stockservice = stockservice;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockDto>>> GetAllStocks()
        {
            var stocks = await _stockservice.GetAllStocksAsync();

            return Ok(stocks);
        }

        [HttpGet("{symbol}")]
        public async Task<ActionResult<StockDto>> GetStockBySymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new { message = "You requested an empty symbol" });
            }

            var stock = await _stockservice.GetStockBySymbol(symbol);

            if(stock == null)
            {
                return NotFound(new { message = "This stock doesn't exist" });
            }

            return Ok(stock);
        }
    }
}
