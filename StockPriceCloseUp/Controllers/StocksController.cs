using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPriceCloseUp.Manager;

namespace StockPriceCloseUp.Controllers
{
    [Authorize]
    public class StocksController : Controller
    {
        private readonly IStockManager _stockManager;

        public StocksController(IStockManager stockManager)
        {
            _stockManager = stockManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Lookup(string? symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                ViewBag.Error = "Please enter a stock symbol.";
                return View("Index");
            }

            try
            {
                var quote = await _stockManager.GetQuoteAsync(symbol);

                if (quote == null)
                {
                    ViewBag.Error = $"Symbol '{symbol.ToUpper()}' not found or not supported.";
                }
                else
                {
                    ViewBag.Symbol = symbol.ToUpper();
                    ViewBag.Quote = quote;
                }
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
            if (string.IsNullOrWhiteSpace(q))
                return Json(Array.Empty<object>());

            var results = await _stockManager.SearchSymbolsAsync(q);
            return Json(results);
        }
    }
}
