using System.ComponentModel.DataAnnotations;
namespace TrainingPlan.Models
{
    public class Membership
    {
        [Key]
        public int MemberId { get; set; }
        public string Status { get; set; } = "Unknown";
        public DateTime RenewalDueDate { get; set; }
    }
}
