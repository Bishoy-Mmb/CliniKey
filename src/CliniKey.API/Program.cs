using CliniKey.API.Middleware;
using CliniKey.Application;
using CliniKey.Application.Constants;
using CliniKey.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CliniKey.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddTransient<GlobalExceptionMiddleware>();
builder.Services.AddTransient<TenantResolutionMiddleware>();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Bearer
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is missing");
if (jwtSecretKey.Length < 32)
    throw new InvalidOperationException("JWT secret key must be at least 32 characters.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanManageTenants,
        p => p.RequireRole(Roles.PlatformOperator));
    options.AddPolicy(Policies.CanInviteStaff,
        p => p.RequireRole(Roles.ClinicAdmin));
    options.AddPolicy(Policies.CanManageStaff,
        p => p.RequireRole(Roles.ClinicAdmin));
    options.AddPolicy(Policies.CanManageAppointments,
        p => p.RequireRole(Roles.ClinicAdmin, Roles.Dentist, Roles.Receptionist));
    options.AddPolicy(Policies.CanViewPatients,
        p => p.RequireRole(Roles.ClinicAdmin, Roles.Dentist, Roles.Receptionist));
    options.AddPolicy(Policies.CanManageBilling,
        p => p.RequireRole(Roles.ClinicAdmin));
    options.AddPolicy(Policies.CanManageTreatmentPlans,
        p => p.RequireRole(Roles.ClinicAdmin, Roles.Dentist));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    string[] roles = [Roles.PlatformOperator, Roles.ClinicAdmin, Roles.Dentist, Roles.Receptionist];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
