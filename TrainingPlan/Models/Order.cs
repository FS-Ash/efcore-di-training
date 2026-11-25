using System.ComponentModel.DataAnnotations;

namespace TrainingPlan.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        [ConcurrencyCheck]
        public string Status { get; set; } = "Pending";
    }
}
