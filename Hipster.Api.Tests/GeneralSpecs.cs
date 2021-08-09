using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using JsonExtensions.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hipster.Api.Tests
{
    public class GeneralSpecs : IClassFixture<PostgresFixture>
    {
        [Fact]
        public async Task GET_request_to_books_endpoint_returns_all_books()
        {
            // Arrange
            using var appFactory = new WebApplicationFactory<Startup>();
            using var client = appFactory.CreateClient();

            // Act & assert
            using var response = await client.GetAsync("/books");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsJsonAsync();

            var books = json.EnumerateArray().ToArray();
            books.Should().HaveCount(2);

            var book1 = books[0];
            book1.GetProperty("title").GetString().Should().Be("The Kite Runner");
            book1.GetProperty("isbn").GetString().Should().Be("978-1-93778-812-0");

            var book2 = books[1];
            book2.GetProperty("title").GetString().Should().Be("The Time Machine");
            book2.GetProperty("isbn").GetString().Should().Be("978-1-93778-812-0");
        }

        [Fact]
        public async Task GET_request_to_books_endpoint_with_book_ID_returns_a_specific_book()
        {
            // Arrange
            using var appFactory = new WebApplicationFactory<Startup>();
            using var client = appFactory.CreateClient();

            // Act & assert
            using var response = await client.GetAsync("/books/1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsJsonAsync();

            json.GetProperty("title").GetString().Should().Be("The Kite Runner");
            json.GetProperty("isbn").GetString().Should().Be("978-1-93778-812-0");
        }

        [Fact]
        public async Task GET_request_to_books_endpoint_with_book_ID_returns_an_error_for_non_existing_book()
        {
            // Arrange
            using var appFactory = new WebApplicationFactory<Startup>();
            using var client = appFactory.CreateClient();

            // Act & assert
            using var response = await client.GetAsync("/books/99999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
