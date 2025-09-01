using Microsoft.AspNetCore.Mvc;
using StockPriceCloseUp.Manager;

namespace StockPriceCloseUp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StocksApiController : ControllerBase
    {
        private readonly IStockManager _stockManager;

        public StocksApiController(IStockManager stockManager)
        {
            _stockManager = stockManager;
        }

        // GET /api/stocksapi/quote?symbol=MSFT
        [HttpGet("quote")]
        public async Task<IActionResult> GetQuote([FromQuery] string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest(new { error = "Symbol is required" });

            var quote = await _stockManager.GetQuoteAsync(symbol);
            if (quote == null)
                return NotFound(new { error = "Symbol not found" });

            return Ok(quote); // JSON
        }

        // GET /api/stocksapi/search?query=MS
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { error = "Query is required" });

            var results = await _stockManager.SearchSymbolsAsync(query);
            return Ok(results); // JSON list
        }
    }
}
