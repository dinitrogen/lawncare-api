using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

/// <summary>
/// Endpoints for ingesting Ecowitt sensor data and serving weather readings
/// to the ESP display and Angular PWA.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly IForecastService _forecastService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        IWeatherService weatherService,
        IForecastService forecastService,
        ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _forecastService = forecastService;
        _logger = logger;
    }

    /// <summary>
    /// Receives a push from the Ecowitt GW1100 gateway (Custom Server protocol).
    /// The GW1100 sends form-encoded data via HTTP POST at a configurable interval.
    /// Configure the gateway's "Customized Server" to POST to this endpoint.
    /// </summary>
    /// <param name="reading">Form fields automatically bound from the Ecowitt POST body.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("ecowitt")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveEcowittData(
        [FromForm] EcowittReading reading, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(reading.PASSKEY))
        {
            _logger.LogWarning("Received Ecowitt push without PASSKEY");
            return BadRequest("Missing PASSKEY");
        }

        var normalised = EcowittMapper.ToWeatherReading(reading);
        await _weatherService.SaveReadingAsync(normalised, ct);

        _logger.LogInformation(
            "Ecowitt data ingested at {Timestamp} from {Station}",
            normalised.Timestamp, normalised.StationType);

        return Ok();
    }

    /// <summary>Returns the most recent weather reading.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(WeatherReading), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var reading = await _weatherService.GetCurrentReadingAsync(ct);
        return reading is null ? NotFound() : Ok(reading);
    }

    /// <summary>
    /// Returns a condensed snapshot suitable for resource-constrained ESP displays.
    /// </summary>
    [HttpGet("display")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisplay(CancellationToken ct)
    {
        var reading = await _weatherService.GetCurrentReadingAsync(ct);
        if (reading is null) return NotFound();

        return Ok(new
        {
            ts = reading.Timestamp,
            tempC = reading.OutdoorTempC,
            feelsLikeC = reading.FeelsLikeC,
            humidityPct = reading.OutdoorHumidityPct,
            windKmh = reading.WindSpeedKmh,
            windGustKmh = reading.WindGustKmh,
            windDir = reading.WindDirectionDeg,
            rainTodayMm = reading.DailyRainMm,
            rainRateMmh = reading.RainRateMmh,
            pressureHpa = reading.PressureRelHpa,
            uvIndex = reading.UvIndex,
            soilMoisturePct = reading.SoilMoisturePct,
            indoorTempC = reading.IndoorTempC
        });
    }

    /// <summary>
    /// Returns historical readings between <paramref name="from"/> and <paramref name="to"/> (UTC).
    /// Defaults to the last 24 hours, newest first. Capped at 500 records.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<WeatherReading>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var end = to ?? DateTime.UtcNow;
        var start = from ?? end.AddHours(-24);
        var capped = Math.Min(limit, 500);

        var readings = await _weatherService.GetHistoryAsync(start, end, capped, ct);
        return Ok(readings);
    }

    /// <summary>
    /// Returns a 7-day weather forecast from the National Weather Service for the given coordinates.
    /// Results are cached for 60 minutes to conserve free API quota.
    /// </summary>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(WeatherForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetForecast(
        [FromQuery] double lat,
        [FromQuery] double lon,
        CancellationToken ct = default)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180)
            return BadRequest("Invalid coordinates");

        var forecast = await _forecastService.GetForecastAsync(lat, lon, cacheDurationMinutes: 60, ct);
        return Ok(forecast);
    }
}
