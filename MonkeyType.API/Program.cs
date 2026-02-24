using Microsoft.EntityFrameworkCore;
using MonkeyType.Infrastructure.Context;
using MonkeyType.Infrastructure.Repositories;
using MonkeyType.Application.Services;
using MonkeyType.Application.IServices;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<MonkeyTypeDatabaseContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStatisticsGameRepository, StatisticsGameRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStatisticsGameService, StatisticsGameService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IJwtService, JwtService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseContext = scope.ServiceProvider.GetRequiredService<MonkeyTypeDatabaseContext>();
    databaseContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

var urls = builder.Configuration["ASPNETCORE_URLS"];
var hasHttpsEndpointConfigured = !string.IsNullOrWhiteSpace(urls)
    && urls.Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Any(url => url.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (hasHttpsEndpointConfigured)
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();