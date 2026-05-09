using CliniKey.API.Middleware;
using CliniKey.Application.Behaviors;
using CliniKey.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register MediatR and Pipeline Behaviors
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(CliniKey.Application.Abstractions.Messaging.ICommand).Assembly);
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Register FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CliniKey.Application.Abstractions.Messaging.ICommand).Assembly);

// Register Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Register Middleware
builder.Services.AddTransient<GlobalExceptionMiddleware>();
builder.Services.AddTransient<TenantResolutionMiddleware>();

// Suppress default ModelState validation to use our custom ValidationBehavior
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

app.Run();
