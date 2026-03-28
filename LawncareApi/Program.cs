using Google.Cloud.Firestore;
using LawncareApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & OpenAPI ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Lawncare API",
        Version = "v1",
        Description = "REST API for weather data ingestion (Ecowitt GW1100/WH51/WN32) " +
                      "and lawn care task management."
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

builder.Services.AddSingleton(_ => FirestoreDb.Create(projectId));
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<ILawnCareTaskService, LawnCareTaskService>();

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
