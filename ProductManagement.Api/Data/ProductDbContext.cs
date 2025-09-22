using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Models;

namespace ProductManagement.Api.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> dbContextOptions)
        : base(dbContextOptions) { }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Product>()
            .HasData(
                new Product
                {
                    Id = 1,
                    Name = "Fanta",
                    Price = 10.0m,
                    Quantity = 10
                },
                new Product
                {
                    Id = 2,
                    Name = "Coke",
                    Price = 20.0m,
                    Quantity = 10
                },
                new Product
                {
                    Id = 3,
                    Name = "Sprite",
                    Price = 30.0m,
                    Quantity = 10
                }
            );
    }
}
