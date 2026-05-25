using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class TenancyModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        return context switch
        {
            AppDbContext appDbContext => (context.GetType(), appDbContext.SharedSchema, designTime),
            SharedDbContext sharedDbContext => (
                context.GetType(),
                sharedDbContext.SharedSchema,
                sharedDbContext.TenantSchemaPrefix,
                designTime),
            _ => (context.GetType(), designTime)
        };
    }
}
