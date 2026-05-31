using FITSync.Infrastructure;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Seeding;
using FITSync.WebAPI.Swagger;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FitSync API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SwaggerBearerOperationFilter>();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration["Cors:AllowedOrigins"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>();

        if (origins.Length > 0)
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitSyncDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            await seeder.SeedAsync();
            logger.LogInformation("Database initialized and seeded successfully.");
            break;
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries)
            {
                logger.LogCritical(ex, "Database initialization failed after {Max} attempts.", maxRetries);
                throw;
            }
            logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in 3s...",
                attempt, maxRetries, ex.Message);
            await Task.Delay(3000);
        }
    }
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        var error = ctx.Features.Get<IExceptionHandlerFeature>();
        var message = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker")
            ? error?.Error.Message ?? "An unexpected error occurred."
            : "An unexpected error occurred.";
        await ctx.Response.WriteAsJsonAsync(new { error = message });
    });
});

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FitSync API v1"));
}

if (!app.Environment.IsEnvironment("Docker"))
    app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
