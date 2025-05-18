using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockTrader.Application.DTOs;
using StockTrader.Application.Services;
using System.Security;
using System.Security.Claims;

namespace StockTrader.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioservice;

        public PortfolioController(IPortfolioService portfolioservice)
        {
            _portfolioservice = portfolioservice;
        }

        [HttpGet]
        public async Task<ActionResult<PortfolioDto>> GetCurrentUserPortfolioAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "User Identification failed" });
            }

            var portfolio = await _portfolioservice.GetPortfolioAsync(userId);

            if(portfolio == null)
            {
                return NotFound(new { message = "Portfolio not found" });
            }

            return Ok(portfolio);
        }
    }
}
