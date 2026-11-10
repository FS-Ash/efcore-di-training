namespace TrainingPlan.Models
{
    public class OrderAuditLog
    {
        public int OrderAuditLogId { get; set; }
        public int OrderId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Order Order { get; set; } = null!;
    }
}
