namespace TrainingPlan.Exceptions;

public class MembershipNotFoundException : Exception
{
    public MembershipNotFoundException(int memberId)
        : base($"Membership for member ID {memberId} not found.")
    {
    }
}