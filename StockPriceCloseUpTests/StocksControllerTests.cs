using Microsoft.AspNetCore.Mvc;
using Moq;
using StockPriceCloseUp.Controllers;
using StockPriceCloseUp.Manager;

namespace StocksControllerTests
{
    public class StocksControllerTests
    {
        private readonly Mock<IStockManager> _stockManagerMock;
        private readonly StocksController _controller;

        public StocksControllerTests()
        {
            _stockManagerMock = new Mock<IStockManager>();
            _controller = new StocksController(_stockManagerMock.Object);
        }

        [Fact]
        public void Index_ReturnsView()
        {
            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Lookup_ReturnsError_WhenSymbolIsNull()
        {
            // Act
            var result = await _controller.Lookup(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal("Please enter a stock symbol.", _controller.ViewBag.Error);
        }

        [Fact]
        public async Task Lookup_ReturnsError_WhenStockManagerReturnsNull()
        {
            // Arrange
            _stockManagerMock.Setup(m => m.GetQuoteAsync("BAD"))
                             .ReturnsAsync((StockQuote?)null);

            // Act
            var result = await _controller.Lookup("BAD");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Contains("not found", _controller.ViewBag.Error as string);
        }

        [Fact]
        public async Task Lookup_SetsViewBag_WhenQuoteReturned()
        {
            // Arrange
            var fakeQuote = new StockQuote { Current = 100, Open = 95 };
            _stockManagerMock.Setup(m => m.GetQuoteAsync("AAPL"))
                             .ReturnsAsync(fakeQuote);

            // Act
            var result = await _controller.Lookup("AAPL");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal("AAPL", _controller.ViewBag.Symbol);
            Assert.Equal(fakeQuote, _controller.ViewBag.Quote);
        }

        [Fact]
        public async Task SearchSymbols_ReturnsEmpty_WhenQueryBlank()
        {
            // Act
            var result = await _controller.SearchSymbols("");

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var data = Assert.IsType<object[]>(json.Value);
            Assert.Empty(data);
        }

        [Fact]
        public async Task SearchSymbols_ReturnsResults_WhenQueryProvided()
        {
            // Arrange
            var results = new List<StockSearchResult>
            {
                new StockSearchResult { Symbol = "AAPL", Description = "Apple Inc." }
            };
            _stockManagerMock.Setup(m => m.SearchSymbolsAsync("AAPL"))
                             .ReturnsAsync(results);

            // Act
            var result = await _controller.SearchSymbols("AAPL");

            // Assert
            var json = Assert.IsType<JsonResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<StockSearchResult>>(json.Value);
            Assert.Single(data);
        }
    }
}
