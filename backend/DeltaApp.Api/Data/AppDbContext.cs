using DeltaApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeltaApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
}
