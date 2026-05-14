using CliniKey.API.Middleware;
using CliniKey.Application.Behaviors;
using CliniKey.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(CliniKey.Application.Abstractions.Messaging.ICommand).Assembly);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(CliniKey.Application.Abstractions.Messaging.ICommand).Assembly);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddTransient<GlobalExceptionMiddleware>();
builder.Services.AddTransient<TenantResolutionMiddleware>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

app.Run();
