using Microsoft.EntityFrameworkCore;

namespace Hipster.Api
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Book> Books { get; set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = 1,
                    Title = "The Kite Runner",
                    Author = "Khaled Hosseini",
                    ISBN = "978-1-93778-812-0",
                    Year = 2014
                },
                new Book
                {
                    Id = 2,
                    Title = "The Time Machine",
                    Author = "H. G. Garside",
                    ISBN = "978-1-93778-812-0",
                    Year = 2002
                }
            );
        }
    }
}