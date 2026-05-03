using Microsoft.EntityFrameworkCore;
using Rtm.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Налаштування Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var host = builder.Configuration["PGHOST"] ?? "localhost";
var port = builder.Configuration["PGPORT"] ?? "5432";
var database = builder.Configuration["PGDATABASE"] ?? "RtmDb";
var user = builder.Configuration["PGUSER"] ?? "postgres";
var password = builder.Configuration["PGPASSWORD"] ?? "postgres";

var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// 2. Налаштовуємо DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

var app = builder.Build();

// Налаштування HTTP пайплайну
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();