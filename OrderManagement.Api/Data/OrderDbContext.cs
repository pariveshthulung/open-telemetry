using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> dbContextOptions)
        : base(dbContextOptions) { }

    public DbSet<Order> Orders { get; set; }
}
