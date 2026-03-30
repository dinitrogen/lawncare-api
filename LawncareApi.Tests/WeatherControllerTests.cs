using LawncareApi.Controllers;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace LawncareApi.Tests;

/// <summary>Stub that returns an empty forecast so controller tests don't need Open-Meteo.</summary>
internal sealed class StubForecastService : IForecastService
{
    public Task<WeatherForecastResponse> GetForecastAsync(
        double lat, double lon, int cacheDurationMinutes = 60, CancellationToken ct = default)
        => Task.FromResult(new WeatherForecastResponse());
}

public class WeatherControllerTests
{
    private static WeatherController CreateController(InMemoryWeatherService? svc = null) =>
        new(svc ?? new InMemoryWeatherService(), new StubForecastService(), NullLogger<WeatherController>.Instance);

    [Fact]
    public async Task GetCurrent_ReturnsNotFound_WhenNoReadingsExist()
    {
        var controller = CreateController();
        var result = await controller.GetCurrent(CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetCurrent_ReturnsLatestReading_AfterIngest()
    {
        var svc = new InMemoryWeatherService();
        await svc.SaveReadingAsync(new WeatherReading
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
            OutdoorTempC = 18.0
        });
        var newer = await svc.SaveReadingAsync(new WeatherReading
        {
            Timestamp = DateTime.UtcNow,
            OutdoorTempC = 20.0
        });

        var controller = CreateController(svc);
        var result = await controller.GetCurrent(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var reading = Assert.IsType<WeatherReading>(ok.Value);
        Assert.Equal(20.0, reading.OutdoorTempC);
    }

    [Fact]
    public async Task ReceiveEcowittData_ReturnsBadRequest_WhenPasskeyMissing()
    {
        var controller = CreateController();
        var ecowitt = new EcowittReading { tempf = 72.0 }; // no PASSKEY
        var result = await controller.ReceiveEcowittData(ecowitt, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ReceiveEcowittData_ReturnsOk_AndPersistsReading()
    {
        var svc = new InMemoryWeatherService();
        var controller = CreateController(svc);

        var ecowitt = new EcowittReading
        {
            PASSKEY = "secret",
            stationtype = "GW1100A",
            tempf = 77.0,
            humidity = 55.0,
            soilmoisture1 = 42
        };

        var result = await controller.ReceiveEcowittData(ecowitt, CancellationToken.None);
        Assert.IsType<OkResult>(result);

        var stored = await svc.GetCurrentReadingAsync();
        Assert.NotNull(stored);
        Assert.Equal(25.0, stored!.OutdoorTempC);
        Assert.NotNull(stored.SoilMoisturePct);
        Assert.Equal(42, stored.SoilMoisturePct![0]);
    }

    [Fact]
    public async Task GetDisplay_ReturnsNotFound_WhenNoReadingsExist()
    {
        var controller = CreateController();
        var result = await controller.GetDisplay(CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetDisplay_ReturnsCondensedPayload()
    {
        var svc = new InMemoryWeatherService();
        await svc.SaveReadingAsync(new WeatherReading
        {
            Timestamp = DateTime.UtcNow,
            OutdoorTempC = 22.5,
            OutdoorHumidityPct = 60,
            DailyRainMm = 5.0,
            UvIndex = 3,
            SoilMoisturePct = [55, 40]
        });

        var controller = CreateController(svc);
        var result = await controller.GetDisplay(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetHistory_ReturnsReadingsInRange()
    {
        var svc = new InMemoryWeatherService();
        var now = DateTime.UtcNow;
        await svc.SaveReadingAsync(new WeatherReading { Timestamp = now.AddHours(-2) });
        await svc.SaveReadingAsync(new WeatherReading { Timestamp = now.AddHours(-1) });
        await svc.SaveReadingAsync(new WeatherReading { Timestamp = now.AddHours(-0.5) });
        await svc.SaveReadingAsync(new WeatherReading { Timestamp = now.AddHours(-3) }); // outside range

        var controller = CreateController(svc);
        var result = await controller.GetHistory(
            from: now.AddHours(-2.1),
            to: now,
            limit: 100,
            ct: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var readings = Assert.IsAssignableFrom<IReadOnlyList<WeatherReading>>(ok.Value);
        Assert.Equal(3, readings.Count);
    }
}
