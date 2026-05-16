using CliniKey.Domain.Entities;
using CliniKey.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CliniKey.Infrastructure.Persistence.Repositories;

internal sealed class InvoiceRepository(AppDbContext context) : IInvoiceRepository
{
    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<Invoice>()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public void Add(Invoice invoice)
    {
        context.Set<Invoice>().Add(invoice);
    }
}
