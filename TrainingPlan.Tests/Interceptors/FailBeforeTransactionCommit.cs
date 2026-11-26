using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class FailBeforeTransactionCommit
    : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context?.Database.CurrentTransaction is not null
            && result > 0)
        {
            throw new DbUpdateException(
                "Simulated failure before transaction commit.");
        }

        return base.SavedChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}