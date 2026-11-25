using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrainingPlan.Data;
using TrainingPlan.Models;
using TrainingPlan.Services;
using Microsoft.Extensions.Logging.Abstractions;
using TrainingPlan.Exceptions;

namespace TrainingPlan.Tests
{
    public class OrderApprovalServiceTests
    {
        [Fact]
        public async Task ApproveOrder_ShouldCommitBothChanges()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            int orderId;

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
                orderId = setupDb.Orders.Single().OrderId;
            }

            await using (var actDb = new AppDbContext(options))
            {
                var service = new OrderApprovalService(actDb, NullLogger<OrderApprovalService>.Instance);
                await service.ApproveOrderAsync(orderId);
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
                .AddInterceptors(new FailOnAuditInsert())
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

                await Assert.ThrowsAsync<DbUpdateException>(
                    () => service.ApproveOrderAsync(orderId));
            }

            await using (var verifyDb = new AppDbContext(options))
            {
                var order = await verifyDb.Orders.SingleAsync(o => o.OrderId == orderId);
                var auditCount = await verifyDb.OrderAuditLogs.CountAsync(a => a.OrderId == orderId);
                Assert.Equal("Pending", order.Status);
                Assert.Equal(0, auditCount);
            }
        }

        [Fact]
        public async Task ApproveOrder_WhenAlreadyApproved_ShouldNotCreateDuplicateAuditLog()
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

            await using (var firstDb = new AppDbContext(options))
            {
                var service = new OrderApprovalService(
                    firstDb,
                    NullLogger<OrderApprovalService>.Instance);

                await service.ApproveOrderAsync(orderId);
            }

            await using (var secondDb = new AppDbContext(options))
            {
                var service = new OrderApprovalService(
                    secondDb,
                    NullLogger<OrderApprovalService>.Instance);

                await service.ApproveOrderAsync(orderId);
            }

            await using (var verifyDb = new AppDbContext(options))
            {
                var order = await verifyDb.Orders
                    .SingleAsync(o => o.OrderId == orderId);

                var auditCount = await verifyDb.OrderAuditLogs
                    .CountAsync(a => a.OrderId == orderId);

                Assert.Equal("Approved", order.Status);
                Assert.Equal(1, auditCount);
            }
        }


        [Fact]
        public async Task ApproveOrder_WhenInvalidStatus_ShouldRejectTransition()
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
                    Status = "Cancelled"
                };

                setupDb.Orders.Add(order);
                await setupDb.SaveChangesAsync();

                orderId = order.OrderId;
            }

            await using (var actDb = new AppDbContext(options))
            {
                var service = new OrderApprovalService(
                    actDb,
                    NullLogger<OrderApprovalService>.Instance);

                await Assert.ThrowsAsync<InvalidOrderStateException>(
                    () => service.ApproveOrderAsync(orderId));
            }

            await using (var verifyDb = new AppDbContext(options))
            {
                var order = await verifyDb.Orders
                    .SingleAsync(o => o.OrderId == orderId);

                var auditCount = await verifyDb.OrderAuditLogs
                    .CountAsync(a => a.OrderId == orderId);

                Assert.Equal("Cancelled", order.Status);
                Assert.Equal(0, auditCount);
            }
        }

        [Fact]
        public async Task ApproveOrder_WhenConcurrentStateChangeOccurs_ShouldNotCreateDuplicateAuditLog()
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

            await using var firstDb = new AppDbContext(options);
            await using var secondDb = new AppDbContext(options);

            var firstOrder = await firstDb.Orders
                .SingleAsync(o => o.OrderId == orderId);

            var secondOrder = await secondDb.Orders
                .SingleAsync(o => o.OrderId == orderId);

            Assert.Equal("Pending", firstOrder.Status);
            Assert.Equal("Pending", secondOrder.Status);

            var firstService = new OrderApprovalService(
                firstDb,
                NullLogger<OrderApprovalService>.Instance);

            var secondService = new OrderApprovalService(
                secondDb,
                NullLogger<OrderApprovalService>.Instance);

            await firstService.ApproveOrderAsync(orderId);

            Assert.Equal("Pending", secondOrder.Status);

            await secondService.ApproveOrderAsync(orderId);

            await using (var verifyDb = new AppDbContext(options))
            {
                var order = await verifyDb.Orders
                    .SingleAsync(o => o.OrderId == orderId);

                var auditCount = await verifyDb.OrderAuditLogs
                    .CountAsync(a => a.OrderId == orderId);

                Assert.Equal("Approved", order.Status);
                Assert.Equal(1, auditCount);
            }
        }

    }
}
