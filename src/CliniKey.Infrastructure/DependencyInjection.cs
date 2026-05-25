using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Repositories;
using CliniKey.Infrastructure.Persistence;
using CliniKey.Infrastructure.Persistence.Repositories;
using CliniKey.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CliniKey.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database") 
            ?? throw new ArgumentNullException("Database connection string not found");

        services
            .AddOptions<TenancyOptions>()
            .Bind(configuration.GetSection(TenancyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

        services.AddScoped<TenantConnectionInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
                .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.AddDbContext<SharedDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextSetter>(sp => sp.GetRequiredService<TenantContext>());
        services.AddSingleton<ITenantSchemaNameGenerator, TenantSchemaNameGenerator>();
        services.AddSingleton<ITenantRegistry, TenantRegistry>();
        services.AddSingleton<ITenantMigrationService, TenantMigrationService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IDentistRepository, DentistRepository>();
        services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IClinicRepository, ClinicRepository>();
        
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        services.AddDbContext<Identity.AuthDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));

        services.AddScoped<Application.Abstractions.Identity.IAuthService, Identity.AuthService>();
        services.AddScoped<Application.Abstractions.Identity.IJwtTokenService, Identity.JwtTokenService>();
        services.AddScoped<Application.Abstractions.Identity.ICurrentUserService, Identity.CurrentUserService>();

        return services;
    }
}
