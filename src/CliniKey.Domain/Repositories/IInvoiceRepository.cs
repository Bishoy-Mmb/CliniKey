using CliniKey.Domain.Entities;

namespace CliniKey.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Invoice invoice);
}
