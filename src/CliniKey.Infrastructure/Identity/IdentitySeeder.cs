using CliniKey.Application.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CliniKey.Infrastructure.Identity;

public static class IdentitySeeder
{
    private static readonly Guid DevTenantId = new("11111111-1111-1111-1111-111111111111");

    public static async Task SeedDevelopmentAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        await SeedRolesAsync(roleManager);
        await SeedPlatformOperatorAsync(userManager, configuration, clock);
    }

    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles = [Roles.PlatformOperator, Roles.ClinicAdmin, Roles.Dentist, Roles.Receptionist];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task SeedPlatformOperatorAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        TimeProvider clock)
    {
        const string defaultEmail = "operator@clinikey.local";
        const string defaultPassword = "CliniKeyDev#12345";
        const string defaultFullName = "Development Platform Operator";

        var email = configuration["DevelopmentSeed:PlatformOperator:Email"] ?? defaultEmail;
        var password = configuration["DevelopmentSeed:PlatformOperator:Password"] ?? defaultPassword;
        var fullName = configuration["DevelopmentSeed:PlatformOperator:FullName"] ?? defaultFullName;

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                TenantId = DevTenantId,
                IsActive = true
            };
            user.InitializeCreatedAt(clock.GetUtcNow().UtcDateTime);

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed development platform operator: {createResult.Errors.First().Description}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, Roles.PlatformOperator))
        {
            var roleResult = await userManager.AddToRoleAsync(user, Roles.PlatformOperator);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign PlatformOperator role: {roleResult.Errors.First().Description}");
            }
        }
    }
}
