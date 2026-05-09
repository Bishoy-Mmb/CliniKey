using CliniKey.Application.Abstractions.Data;
using CliniKey.Domain.Repositories;
using CliniKey.Infrastructure.Persistence;
using CliniKey.Infrastructure.Persistence.Repositories;
using CliniKey.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CliniKey.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database") 
            ?? throw new ArgumentNullException("Database connection string not found");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        
        services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));

        return services;
    }
}
