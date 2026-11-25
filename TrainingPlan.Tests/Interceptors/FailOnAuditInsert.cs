using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TrainingPlan.Models;

public sealed class FailOnAuditInsert : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is not null &&
            context.ChangeTracker.Entries<OrderAuditLog>()
                .Any(entry => entry.State == EntityState.Added))
        {
            throw new DbUpdateException(
                "Simulated audit insertion failure.");
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}


