using Microsoft.AspNetCore.Mvc;
using TrainingPlan.Services;
namespace TrainingPlan.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderApprovalService _orderApprovalService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrderApprovalService orderApprovalService, ILogger<OrdersController> logger)
        {
            _orderApprovalService = orderApprovalService;
            _logger = logger;
        }

        [HttpPost("{orderId:int}/approve")]
        [HttpPost("{orderId:int}/integrated-approve")]
        public async Task<IActionResult> ApproveOrder(int orderId, [FromQuery] bool simulateFailure = false)
        {
            var correlationId = HttpContext.TraceIdentifier;

            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });
            _logger.LogInformation("Order approval requested. CorrelationId: {CorrelationId}, OrderId: {OrderId}",correlationId,orderId);

            try
            {
                await _orderApprovalService.ApprovedOrderAsync(orderId, simulateFailure);

                _logger.LogInformation("Order approval completed. CorrelationId: {CorrelationId}, OrderId: {OrderId}", correlationId, orderId);
                
                return Ok(new
                {
                    orderId = orderId,
                    Status = "Approved"
                });
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Order approval failed. CorrelationId: {CorrelationId}, OrderId: {OrderId}", correlationId, orderId);

                throw;
            }
        }
    }
}
