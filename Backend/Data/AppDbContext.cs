using Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

//AppDbContext is being used to check and parse email and username that aren't unique
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
    }

}