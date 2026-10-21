using Microsoft.EntityFrameworkCore;

namespace TedarikCore.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
