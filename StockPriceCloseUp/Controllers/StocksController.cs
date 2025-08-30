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
                ViewBag.Error = "Please enter a symbol.";
                return View("Index");
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={_finnhubApiKey}";
            var response = await client.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var currentPrice = doc.RootElement.GetProperty("c").GetDecimal();

            ViewBag.Symbol = symbol.ToUpper();
            ViewBag.Price = currentPrice;

            return View("Index");
        }
    }
}
