using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Keyless.Infrastructure.Context;
using Keyless.Infrastructure.Repositories;
using Keyless.Application.Services;
using Keyless.Application.IServices;
using Keyless.Domain.IRepositories;
using Keyless.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Configuration["Observability:ServiceName"] ?? builder.Environment.ApplicationName;
var correlationHeaderName = builder.Configuration["Observability:CorrelationHeaderName"] ?? "X-Correlation-ID";

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.ParentId
        | ActivityTrackingOptions.Baggage
        | ActivityTrackingOptions.Tags;
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.DisallowCredentials();
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
var resolvedConnectionString = PostgresConnectionStringResolver.Resolve(connectionString);

builder.Services.AddDbContext<KeylessDatabaseContext>(options =>
    options.UseNpgsql(resolvedConnectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStatisticsGameRepository, StatisticsGameRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStatisticsGameService, StatisticsGameService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserStatsAggregateRepository, UserStatsAggregateRepository>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
if (jwtKey.StartsWith("PLACEHOLDER", StringComparison.OrdinalIgnoreCase) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be a strong secret of at least 32 characters.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.Logger.LogInformation(
    "Starting {ServiceName} in {Environment} environment.",
    serviceName,
    app.Environment.EnvironmentName);

app.UseExceptionHandler(exceptionApplication =>
{
    exceptionApplication.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is not null)
        {
            context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Keyless.API.UnhandledException")
                .LogError(
                    exception,
                    "Unhandled exception while processing {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Unexpected server error",
            detail: app.Environment.IsDevelopment() ? exception?.Message : null)
            .ExecuteAsync(context);
    });
});

app.Use(async (context, next) =>
{
    var requestLogger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Keyless.API.Request");

    var correlationId = context.Request.Headers[correlationHeaderName].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("n");
    }

    context.TraceIdentifier = correlationId;
    context.Response.Headers[correlationHeaderName] = correlationId;

    var stopwatch = Stopwatch.StartNew();

    using (requestLogger.BeginScope(new Dictionary<string, object?>
    {
        ["Service"] = serviceName,
        ["CorrelationId"] = correlationId,
        ["RequestPath"] = context.Request.Path.Value
    }))
    {
        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();

            var userId = context.User.FindFirst("sub")?.Value ?? "anonymous";
            var logLevel = context.Request.Path.StartsWithSegments("/health")
                ? LogLevel.Debug
                : LogLevel.Information;

            requestLogger.Log(
                logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms for {UserId}.",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                userId);
        }
    }
});

using (var scope = app.Services.CreateScope())
{
    var databaseContext = scope.ServiceProvider.GetRequiredService<KeylessDatabaseContext>();

    try
    {
        app.Logger.LogInformation("Applying database migrations for {ServiceName}.", serviceName);
        databaseContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations completed for {ServiceName}.", serviceName);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database migration failed for {ServiceName}.", serviceName);
        throw;
    }
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

app.UseCors("Default");

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self';");
    await next.Invoke();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = serviceName,
    environment = app.Environment.EnvironmentName
}));
app.MapControllers();

app.Run();