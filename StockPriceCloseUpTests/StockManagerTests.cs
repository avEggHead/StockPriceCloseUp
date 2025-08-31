using Moq;
using Moq.Protected;
using StockPriceCloseUp.Manager;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace StockPriceCloseUpTests
{
    public class StockManagerTests
    {
        private StockManager CreateStockManager(string jsonResponse)
        {
            // Fake HttpMessageHandler
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var client = new HttpClient(handlerMock.Object);

            // Fake IHttpClientFactory
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            // Fake configuration
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Finnhub-ApiKey", "fake-key" }
                })
                .Build();

            return new StockManager(factoryMock.Object, config);
        }

        [Fact]
        public async Task GetQuoteAsync_ReturnsQuote_WhenValidJson()
        {
            // Arrange
            var json = @"{
                ""c"": 150.25,
                ""o"": 149.00,
                ""h"": 152.00,
                ""l"": 148.50,
                ""pc"": 148.00,
                ""t"": 1670000000
            }";
            var manager = CreateStockManager(json);

            // Act
            var result = await manager.GetQuoteAsync("AAPL");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(150.25m, result!.Current);
            Assert.Equal(149.00m, result.Open);
            Assert.Equal(152.00m, result.High);
            Assert.Equal(148.50m, result.Low);
            Assert.Equal(148.00m, result.PreviousClose);
            Assert.Equal(1670000000, result.Timestamp);
        }

        [Fact]
        public async Task GetQuoteAsync_ReturnsNull_WhenCurrentIsZero()
        {
            // Arrange
            var json = @"{ ""c"": 0, ""o"": 149, ""h"": 152, ""l"": 148, ""pc"": 148, ""t"": 1670000000 }";
            var manager = CreateStockManager(json);

            // Act
            var result = await manager.GetQuoteAsync("BAD");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SearchSymbolsAsync_ReturnsResults()
        {
            // Arrange
            var json = @"{
                ""result"": [
                    { ""symbol"": ""AAPL"", ""description"": ""Apple Inc."" },
                    { ""symbol"": ""MSFT"", ""description"": ""Microsoft Corp"" }
                ]
            }";
            var manager = CreateStockManager(json);

            // Act
            var results = await manager.SearchSymbolsAsync("A");

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("AAPL", results[0].Symbol);
            Assert.Equal("Apple Inc.", results[0].Description);
        }

        [Fact]
        public async Task SearchSymbolsAsync_ReturnsEmpty_WhenNoResults()
        {
            // Arrange
            var json = @"{ ""result"": [] }";
            var manager = CreateStockManager(json);

            // Act
            var results = await manager.SearchSymbolsAsync("Z");

            // Assert
            Assert.Empty(results);
        }
    }
}
