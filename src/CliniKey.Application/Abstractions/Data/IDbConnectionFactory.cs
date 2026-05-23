using System.Data;

namespace CliniKey.Application.Abstractions.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    IDbConnection CreateTenantConnection();
}
