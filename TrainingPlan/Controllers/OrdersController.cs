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
        public async Task<IActionResult> ApproveOrder(int orderId)
        {
            _logger.LogInformation(
                "Order approval requested. OrderId: {OrderId}",
                orderId);

            await _orderApprovalService.ApproveOrderAsync(orderId);

            _logger.LogInformation(
                "Order approval completed. OrderId: {OrderId}",
                orderId);

            return Ok(new
            {
                orderId = orderId,
                Status = "Approved"
            });
        }
    }
}
