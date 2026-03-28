using LawncareApi.Models;

namespace LawncareApi.Tests;

public class WeatherReadingTests
{
    [Fact]
    public void FeelsLikeC_ReturnsOutdoorTemp_WhenWindSpeedIsNull()
    {
        var reading = new WeatherReading { OutdoorTempC = 15.0, WindSpeedKmh = null };
        Assert.Equal(15.0, reading.FeelsLikeC);
    }

    [Fact]
    public void FeelsLikeC_ReturnsNull_WhenBothFieldsNull()
    {
        var reading = new WeatherReading { OutdoorTempC = null, WindSpeedKmh = null };
        Assert.Null(reading.FeelsLikeC);
    }

    [Fact]
    public void FeelsLikeC_IsLessThanActualTemp_WhenColdAndWindy()
    {
        // Wind chill should make feels-like colder than actual in cold + wind scenario
        var reading = new WeatherReading { OutdoorTempC = -5.0, WindSpeedKmh = 30.0 };
        Assert.True(reading.FeelsLikeC < reading.OutdoorTempC);
    }

    [Fact]
    public void FeelsLikeC_IsComputed_WhenBothFieldsPresent()
    {
        var reading = new WeatherReading { OutdoorTempC = 10.0, WindSpeedKmh = 20.0 };
        Assert.NotNull(reading.FeelsLikeC);
    }
}
