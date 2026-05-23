using CliniKey.SharedKernel.Interfaces;
using CliniKey.SharedKernel.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly IPublisher _publisher;
    private readonly TimeProvider _clock;

    public UnitOfWork(AppDbContext dbContext, IPublisher publisher, TimeProvider clock)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        var domainEvents = CollectDomainEvents();

        var result = await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(domainEvents, cancellationToken);

        return result;
    }

    private void UpdateAuditableEntities()
    {
        var entries = _dbContext.ChangeTracker.Entries<IAuditableEntity>();
        var now = _clock.GetUtcNow().UtcDateTime;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).CurrentValue = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditableEntity.UpdatedAtUtc)).CurrentValue = now;
            }
        }
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        return _dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .ToList();
    }

    private async Task PublishDomainEventsAsync(List<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
