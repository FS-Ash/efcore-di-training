using Microsoft.EntityFrameworkCore;
using TrainingPlan.Models;
namespace TrainingPlan.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Membership> Memberships { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderAuditLog> OrderAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderAuditLog>()
            .HasOne(a => a.Order)
            .WithMany()
            .HasForeignKey(a => a.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
