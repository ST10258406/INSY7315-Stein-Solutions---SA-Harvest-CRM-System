namespace CRM.Infrastructure.Persistence.Interceptors;

using CRM.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Automatically stamps CreatedAt/UpdatedAt on every entity implementing IHasUpdatedAt
/// before SaveChanges runs. No handler should ever manually set UpdatedAt.
///
/// InteractionLog and AuditLog do not implement IHasUpdatedAt (append-only tables,
/// no UpdatedAt column), so they are correctly skipped here automatically.
/// </summary>
public class UpdatedAtInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateTimestamps(DbContext? context)
    {
        if (context is null) return;

        context.ChangeTracker.DetectChanges(); // <-- add this

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IHasUpdatedAt>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }

            if (entry.State == EntityState.Added && entry.Entity is BaseEntity baseEntity)
            {
                baseEntity.CreatedAt = now;
                baseEntity.UpdatedAt = now;
            }
        }
    }
}
