using Microsoft.EntityFrameworkCore;
using TrainingPlan.Data;
using TrainingPlan.Models;

namespace TrainingPlan.Services
{
    public class OrderApprovalService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OrderApprovalService> _logger;

        public OrderApprovalService(AppDbContext db, ILogger<OrderApprovalService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task ApprovedOrderAsync(int orderId, bool simulateFailure = false)
        {

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId ==orderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {orderId} not found.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var previousStatus = order.Status;

                order.Status = "Approved";
                await _db.SaveChangesAsync();

                if (simulateFailure)
                {
                    throw new Exception("Simulated failure after order approval.");
                }

                var auditLog = new OrderAuditLog
                {
                    OrderId = order.OrderId,
                    Action = "Approved",
                    Timestamp = DateTime.UtcNow
                };

                _db.OrderAuditLogs.Add(auditLog);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Order approval committed. OrderId: {OrderId}, Previous Status: {PreviousStatus}, New Status: {NewStatus}", orderId, previousStatus, order.Status);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
