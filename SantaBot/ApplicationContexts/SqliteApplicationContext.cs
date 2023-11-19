using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SantaBot.Models;
using Serilog;

namespace SantaBot.ApplicationContexts;

public sealed class SqliteApplicationContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Settings> Settings => Set<Settings>();
    public SqliteApplicationContext() => Database.EnsureCreated();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source = Users.db");
        optionsBuilder.UseLoggerFactory(LoggerFactory.Create(x => x.AddSerilog(dispose: true)));
    }
}