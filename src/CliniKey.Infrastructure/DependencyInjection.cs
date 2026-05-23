using CliniKey.Application.Abstractions.Data;
using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Repositories;
using CliniKey.Infrastructure.Persistence;
using CliniKey.Infrastructure.Persistence.Repositories;
using CliniKey.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CliniKey.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database") 
            ?? throw new ArgumentNullException("Database connection string not found");

        services.Configure<TenancyOptions>(configuration.GetSection(TenancyOptions.SectionName));
        services.PostConfigure<TenancyOptions>(options => options.ConnectionString = connectionString);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TenancyOptions>>().Value);

        services.AddScoped<TenantConnectionInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>()));

        services.AddDbContext<SharedDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextSetter>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantRegistry>(sp => new TenantRegistry(
            connectionString,
            sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            sp.GetRequiredService<TenancyOptions>()));
        services.AddScoped<ITenantMigrationService>(sp => new TenantMigrationService(
            connectionString,
            sp.GetRequiredService<TenancyOptions>()));
        services.AddScoped<ITenantProvisioningService>(sp => new TenantProvisioningService(
            sp.GetRequiredService<ITenantMigrationService>(),
            sp.GetRequiredService<SharedDbContext>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IOptions<TenancyOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TenantProvisioningService>>()));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IDentistRepository, DentistRepository>();
        services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IClinicRepository, ClinicRepository>();
        
        services.AddScoped<IDbConnectionFactory>(sp => new DbConnectionFactory(
            connectionString,
            sp.GetRequiredService<ITenantContext>()));

        services.AddDbContext<Identity.AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<Application.Abstractions.Identity.IAuthService, Identity.AuthService>();
        services.AddScoped<Application.Abstractions.Identity.IJwtTokenService, Identity.JwtTokenService>();
        services.AddScoped<Application.Abstractions.Identity.ICurrentUserService, Identity.CurrentUserService>();

        return services;
    }
}
