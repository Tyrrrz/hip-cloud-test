using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hipster.Api
{
    [Route("[controller]")]
    [ApiController]
    public class BooksController : Controller
    {
        private readonly DatabaseContext _db;

        public BooksController(DatabaseContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Book[]), 200)]
        public async Task<IActionResult> GetBooks()
        {
            var books = await _db.Books.ToArrayAsync();
            return Ok(books);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Book), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<IActionResult> GetBook(int id)
        {
            if (id == 2)
                throw new System.Exception("oops");

            var book = await _db.Books.FindAsync(id);

            if (book is null)
                return NotFound();

            return Ok(book);
        }
    }
}