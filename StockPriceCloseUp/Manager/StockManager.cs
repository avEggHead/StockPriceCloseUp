using System.Text.Json;

namespace StockPriceCloseUp.Manager
{
    public interface IStockManager
    {
        Task<StockQuote?> GetQuoteAsync(string symbol);
        Task<List<StockSearchResult>> SearchSymbolsAsync(string query);
    }

    public class StockManager : IStockManager
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _finnhubApiKey;

        public StockManager(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _finnhubApiKey = config["Finnhub-ApiKey"];
            _finnhubApiKey = "d2pk6dpr01qnf9nlehi0d2pk6dpr01qnf9nlehig";
        }

        public async Task<StockQuote?> GetQuoteAsync(string symbol)
        {
            var client = _httpClientFactory.CreateClient();
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

            return quote.Current == 0 ? null : quote;
        }

        public async Task<List<StockSearchResult>> SearchSymbolsAsync(string query)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://finnhub.io/api/v1/search?q={query}&token={_finnhubApiKey}";

            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            return doc.RootElement.GetProperty("result")
                .EnumerateArray()
                .Select(x => new StockSearchResult
                {
                    Symbol = x.GetProperty("symbol").GetString()!,
                    Description = x.GetProperty("description").GetString()!
                })
                .ToList();
        }
    }

    public class StockSearchResult
    {
        public string Symbol { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
