using Microsoft.EntityFrameworkCore;
using TrainingPlan.Data;
using TrainingPlan.Models;
using TrainingPlan.Exceptions;

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

        public async Task ApproveOrderAsync(int orderId)
        {

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                throw new OrderNotFoundException(orderId);
            }

            if (order.Status == "Approved")
            {
                return;
            }

            if (order.Status != "Pending")
            {
                throw new InvalidOrderStateException(orderId, order.Status);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var previousStatus = order.Status;

                order.Status = "Approved";

                // Two separate saves are intentional for the Day 5
                // transaction exercise. The first persists the order
                // update within the transaction, allowing an audit
                // insertion failure to demonstrate rollback of an
                // already-executed database operation.
                //
                // In production, these changes would normally be
                // persisted using a single SaveChangesAsync() call.

                await _db.SaveChangesAsync();

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
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Order approval rolled back due to concurrency conflict. OrderId: {OrderId}", orderId);
                await _db.Entry(order).ReloadAsync();
                if (_db.Entry(order).State != EntityState.Detached && order.Status == "Approved")
                {
                    _logger.LogInformation("Order already approved by another operation, OrderId: {OrderId}", orderId);
                    return;
                }
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Order approval rolled back. OrderId: {OrderId}", orderId);
                throw;
            }
        }
    }
}
