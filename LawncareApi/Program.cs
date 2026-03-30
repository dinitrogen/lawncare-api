using Google.Api.Gax;
using Google.Cloud.Firestore;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & OpenAPI ────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Lawncare API",
        Version = "v1",
        Description = "REST API for weather data ingestion (Ecowitt GW1100), " +
                      "lawn care management, and GDD tracking."
    });
});

// ── CORS – allow Angular PWA origin(s) ──────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Firestore ────────────────────────────────────────────────────────────────
var projectId = builder.Configuration["Firestore:ProjectId"]
    ?? throw new InvalidOperationException(
        "Firestore:ProjectId is not configured. " +
        "Set it in appsettings.json or via FIRESTORE__PROJECTID environment variable.");

var credentialFile = builder.Configuration["Firestore:CredentialFile"];
builder.Services.AddSingleton(_ =>
{
    var dbBuilder = new FirestoreDbBuilder { ProjectId = projectId };
    if (!string.IsNullOrEmpty(credentialFile))
        dbBuilder.CredentialsPath = credentialFile;
    return dbBuilder.Build();
});

// ── Firebase JWT Authentication ──────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{projectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

// ── HttpClient for external APIs (NWS, Discord, Zippopotam.us) ──────────
builder.Services.AddHttpClient();

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<ILawnCareTaskService, LawnCareTaskService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ITreatmentService, TreatmentService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<ISoilTestService, SoilTestService>();
builder.Services.AddScoped<IGddApiService, GddApiService>();
builder.Services.AddScoped<IForecastService, ForecastService>();
builder.Services.AddScoped<ISeasonalService, SeasonalService>();
builder.Services.AddScoped<DiscordNotificationService>();

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Prevent Firebase Hosting CDN from caching API responses
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-store";
    await next();
});

// Log all incoming requests (path, method, content-type) for debugging gateway issues
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Request: {Method} {Path} Content-Type={ContentType} Content-Length={ContentLength} From={RemoteIp}",
        context.Request.Method,
        context.Request.Path,
        context.Request.ContentType ?? "(none)",
        context.Request.ContentLength,
        context.Connection.RemoteIpAddress);
    await next();
    logger.LogInformation("Response: {StatusCode} for {Method} {Path}",
        context.Response.StatusCode, context.Request.Method, context.Request.Path);
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ── Ensures all DateTime values round-trip as UTC for Firestore compatibility ─
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dt = reader.GetDateTime();
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc), // Unspecified → treat as UTC
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteStringValue(utc.ToString("O"));
    }
}
