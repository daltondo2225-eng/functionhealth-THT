using Microsoft.EntityFrameworkCore;

namespace TodoApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<TodoItem>(b =>
        {
            b.HasIndex(t => t.UserId);
            b.Property(t => t.Title).IsRequired().HasMaxLength(200);
            b.Property(t => t.Description).HasMaxLength(2000);
            b.HasOne(t => t.User)
                .WithMany(u => u.Todos)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
