// In StockTrader.API/Controllers/TestController.cs
using Microsoft.AspNetCore.Mvc;

namespace StockTrader.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Pong from TestController!" });
        }
    }
}