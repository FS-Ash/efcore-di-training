namespace TrainingPlan.Exceptions
{
    public class InvalidOrderStateException : Exception
    {
        public InvalidOrderStateException(int orderId, string status)
            : base($"Order with ID {orderId} cannot be approved from status '{status}'.")
        {
        }
    }
}
