using LawncareApi.Models;
using LawncareApi.Services;

namespace LawncareApi.Tests;

public class EcowittMapperTests
{
    [Fact]
    public void ToWeatherReading_ConvertsTemperatureFromFahrenheitToCelsius()
    {
        var src = new EcowittReading { tempf = 32.0 };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Equal(0.0, result.OutdoorTempC);
    }

    [Fact]
    public void ToWeatherReading_ConvertsBoilingPointCorrectly()
    {
        var src = new EcowittReading { tempf = 212.0 };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Equal(100.0, result.OutdoorTempC);
    }

    [Fact]
    public void ToWeatherReading_ConvertsWindSpeedFromMphToKmh()
    {
        var src = new EcowittReading { windspeedmph = 100.0 };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Equal(160.93, result.WindSpeedKmh);
    }

    [Fact]
    public void ToWeatherReading_ConvertsRainFromInchesToMm()
    {
        var src = new EcowittReading { dailyrainin = 1.0 };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Equal(25.4, result.DailyRainMm);
    }

    [Fact]
    public void ToWeatherReading_ConvertsPressureFromInHgToHpa()
    {
        var src = new EcowittReading { baromrelin = 29.92 };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Equal(1013.25, result.PressureRelHpa!.Value, precision: 1);
    }

    [Fact]
    public void ToWeatherReading_MapsSoilMoisturePct_WhenChannelsPresent()
    {
        var src = new EcowittReading
        {
            soilmoisture1 = 45,
            soilmoisture2 = 62,
            soilmoisture3 = 38
        };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.NotNull(result.SoilMoisturePct);
        Assert.Equal(3, result.SoilMoisturePct!.Count);
        Assert.Equal(45, result.SoilMoisturePct[0]);
        Assert.Equal(62, result.SoilMoisturePct[1]);
        Assert.Equal(38, result.SoilMoisturePct[2]);
    }

    [Fact]
    public void ToWeatherReading_SoilMoisturePct_IsNull_WhenNoChannelsPresent()
    {
        var src = new EcowittReading { tempf = 70.0 };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Null(result.SoilMoisturePct);
    }

    [Fact]
    public void ToWeatherReading_ParsesDateUtcFromEcowittFormat()
    {
        var src = new EcowittReading { dateutc = "2025-06-15 12:00:00" };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Equal(2025, result.Timestamp.Year);
        Assert.Equal(6, result.Timestamp.Month);
        Assert.Equal(15, result.Timestamp.Day);
    }

    [Fact]
    public void ToWeatherReading_UsesUtcNow_WhenDateUtcIsNullOrInvalid()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var src = new EcowittReading { dateutc = "not-a-date" };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.True(result.Timestamp >= before);
    }

    [Fact]
    public void ToWeatherReading_SetsStationType()
    {
        var src = new EcowittReading { stationtype = "GW1100A_V2.1.8" };
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Equal("GW1100A_V2.1.8", result.StationType);
    }

    [Fact]
    public void ToWeatherReading_NullFields_ResultInNullProperties()
    {
        var src = new EcowittReading();
        var result = EcowittMapper.ToWeatherReading(src);
        Assert.Null(result.OutdoorTempC);
        Assert.Null(result.WindSpeedKmh);
        Assert.Null(result.DailyRainMm);
        Assert.Null(result.PressureRelHpa);
    }
}
