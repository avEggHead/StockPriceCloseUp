using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
                var currentPrice = doc.RootElement.GetProperty("c").GetDecimal();

                // Finnhub returns 0 if symbol not found or no price available
                if (currentPrice == 0)
                {
                    ViewBag.Error = $"Symbol '{symbol.ToUpper()}' not found or not supported.";
                    return View("Index");
                }

                ViewBag.Symbol = symbol.ToUpper();
                ViewBag.Price = currentPrice;
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
