
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrainingPlan.Data;
using TrainingPlan.Models;

namespace TrainingPlan.Tests;

public class Day5IntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IntegratedApproval_ShouldHandleCommitAndRollback(
        bool simulateFailure)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<
                        Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<AppDbContext>>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(connection));

                });
            });

        // Create database and seed an order.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.Orders.Add(new Order { Status = "Pending" });
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/orders/1/integrated-approve?simulateFailure={simulateFailure.ToString().ToLowerInvariant()}",
            null);

        // Verify database state using a fresh DbContext.
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await verifyDb.Orders.SingleAsync();
        var auditCount = await verifyDb.OrderAuditLogs.CountAsync();

        if (simulateFailure)
        {
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("Pending", order.Status);
            Assert.Equal(0, auditCount);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Approved", order.Status);
            Assert.Equal(1, auditCount);
        }
    }
}



