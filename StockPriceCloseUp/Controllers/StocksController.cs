using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MyMvcApp.Controllers
{
    [Authorize]
    public class StocksController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _finnhubApiKey;

        public StocksController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _finnhubApiKey = config["Finnhub-ApiKey"];
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Lookup(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                ViewBag.Error = "Please enter a stock symbol.";
                return View("Index");
            }

            var client = _httpClientFactory.CreateClient();

            try
            {
                var url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={_finnhubApiKey}";
                var response = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);

                var quote = new StockQuote
                {
                    Current = doc.RootElement.GetProperty("c").GetDecimal(),
                    Open = doc.RootElement.GetProperty("o").GetDecimal(),
                    High = doc.RootElement.GetProperty("h").GetDecimal(),
                    Low = doc.RootElement.GetProperty("l").GetDecimal(),
                    PreviousClose = doc.RootElement.GetProperty("pc").GetDecimal(),
                    Timestamp = doc.RootElement.GetProperty("t").GetInt64()
                };

                if (quote.Current == 0)
                {
                    ViewBag.Error = $"Symbol '{symbol.ToUpper()}' not found or not supported.";
                    return View("Index");
                }

                ViewBag.Symbol = symbol.ToUpper();
                ViewBag.Quote = quote;
            }
            catch
            {
                ViewBag.Error = $"Could not retrieve data for '{symbol.ToUpper()}'.";
            }

            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> SearchSymbols(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());

            var client = _httpClientFactory.CreateClient();
            var url = $"https://finnhub.io/api/v1/search?q={q}&token={_finnhubApiKey}";
            var response = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var results = doc.RootElement.GetProperty("result")
                .EnumerateArray()
                .Select(x => new
                {
                    symbol = x.GetProperty("symbol").GetString(),
                    description = x.GetProperty("description").GetString()
                })
                .ToList();

            return Json(results);
        }

    }
}
