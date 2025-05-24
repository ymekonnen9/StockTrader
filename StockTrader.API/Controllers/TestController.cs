using Microsoft.AspNetCore.Mvc;
namespace StockTrader.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Expects /api/test
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        public TestController(ILogger<TestController> logger) { _logger = logger; } // Add logger

        [HttpGet("ping")] // Expects /api/test/ping
        public IActionResult Ping()
        {
            _logger.LogInformation("TestController PING endpoint hit!");
            return Ok(new { message = "Pong from TestController!" });
        }
        [HttpGet] // Expects /api/test
        public IActionResult GetBase()
        {
            _logger.LogInformation("TestController GET BASE endpoint hit!");
            return Ok(new { message = "Base GET from TestController!" });
        }
    }
}