using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrainingPlan.Data;
using TrainingPlan.Models;
using TrainingPlan.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace TrainingPlan.Tests
{
    public class OrderApprovalServiceTests
    {
        [Fact]
        public async Task ApproveOrder_ShouldCommitBothChanges()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using (var setupDb = new AppDbContext(options))
            {
                await setupDb.Database.EnsureCreatedAsync();
                setupDb.Orders.Add(new Order
                {
                    Status = "Pending"
                });
                await setupDb.SaveChangesAsync();
            }

            await using (var actDb = new AppDbContext(options))
            {
                var service = new OrderApprovalService(actDb, NullLogger<OrderApprovalService>.Instance);
                await service.ApprovedOrderAsync(1);
            }

            await using (var verifyDb = new AppDbContext(options))
            {
                var order = await verifyDb.Orders.SingleAsync(o => o.OrderId == 1);
                var audit = await verifyDb.OrderAuditLogs.SingleAsync(a => a.OrderId == 1);
                Assert.Equal("Approved", order.Status);
                Assert.Equal("Approved", audit.Action);
            }
        }

        [Fact]
        public async Task ApproveOrder_WhenFailureOccurs_ShouldRollback()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            int orderId;

            await using (var setupDb = new AppDbContext(options))
            {
                await setupDb.Database.EnsureCreatedAsync();
                var order = new Order
                {
                    Status = "Pending"
                };
                setupDb.Orders.Add(order);

                await setupDb.SaveChangesAsync();
                orderId = order.OrderId;
            }

            await using (var actDb = new AppDbContext(options))
            {
                var service = new OrderApprovalService(actDb, NullLogger<OrderApprovalService>.Instance);

                await Assert.ThrowsAsync<Exception>(
                    () => service.ApprovedOrderAsync(orderId, simulateFailure: true));
            }

            await using (var verifyDb = new AppDbContext(options))
            {
                var order = await verifyDb.Orders.SingleAsync(o => o.OrderId == orderId);
                var auditCount = await verifyDb.OrderAuditLogs.CountAsync(a => a.OrderId == orderId);
                Assert.Equal("Pending", order.Status);
                Assert.Equal(0, auditCount);
            }
        }
    }
}
