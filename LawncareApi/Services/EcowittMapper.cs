using LawncareApi.Models;

namespace LawncareApi.Services;

/// <summary>
/// Maps a raw <see cref="EcowittReading"/> (imperial units) into a normalised
/// <see cref="WeatherReading"/> (metric units).
/// </summary>
public static class EcowittMapper
{
    /// <summary>Converts Fahrenheit to Celsius, rounded to 2 decimal places.</summary>
    private static double FtoC(double f) => Math.Round((f - 32.0) / 1.8, 2);
    /// <summary>Converts miles per hour to kilometres per hour, rounded to 2 decimal places.</summary>
    private static double MphToKmh(double mph) => Math.Round(mph * 1.60934, 2);
    /// <summary>Converts inches to millimetres, rounded to 2 decimal places.</summary>
    private static double InToMm(double inch) => Math.Round(inch * 25.4, 2);
    /// <summary>Converts inches of mercury to hectopascals, rounded to 2 decimal places.</summary>
    private static double InHgToHpa(double inHg) => Math.Round(inHg * 33.8639, 2);

    public static WeatherReading ToWeatherReading(EcowittReading src)
    {
        var timestamp = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(src.dateutc) &&
            DateTime.TryParse(src.dateutc, out var parsed))
            timestamp = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        var soilMoisture = new List<int?> {
            src.soilmoisture1, src.soilmoisture2, src.soilmoisture3, src.soilmoisture4,
            src.soilmoisture5, src.soilmoisture6, src.soilmoisture7, src.soilmoisture8
        }
        .Where(v => v.HasValue)
        .Select(v => v!.Value)
        .ToList();

        var soilTemp = new List<double?> {
            src.tf_ch1, src.tf_ch2, src.tf_ch3, src.tf_ch4,
            src.tf_ch5, src.tf_ch6, src.tf_ch7, src.tf_ch8
        }
        .Where(v => v.HasValue)
        .Select(v => FtoC(v!.Value))
        .ToList();

        return new WeatherReading
        {
            Timestamp = timestamp,
            StationType = src.stationtype,
            OutdoorTempC = src.tempf.HasValue ? FtoC(src.tempf.Value) : null,
            OutdoorHumidityPct = src.humidity,
            WindSpeedKmh = src.windspeedmph.HasValue ? MphToKmh(src.windspeedmph.Value) : null,
            WindGustKmh = src.windgustmph.HasValue ? MphToKmh(src.windgustmph.Value) : null,
            WindDirectionDeg = src.winddir,
            RainRateMmh = src.rainratein.HasValue ? InToMm(src.rainratein.Value) : null,
            DailyRainMm = src.dailyrainin.HasValue ? InToMm(src.dailyrainin.Value) : null,
            WeeklyRainMm = src.weeklyrainin.HasValue ? InToMm(src.weeklyrainin.Value) : null,
            MonthlyRainMm = src.monthlyrainin.HasValue ? InToMm(src.monthlyrainin.Value) : null,
            PressureRelHpa = src.baromrelin.HasValue ? InHgToHpa(src.baromrelin.Value) : null,
            PressureAbsHpa = src.baromabsin.HasValue ? InHgToHpa(src.baromabsin.Value) : null,
            SolarRadiationWm2 = src.solarradiation,
            UvIndex = src.uv,
            IndoorTempC = src.tempinf.HasValue ? FtoC(src.tempinf.Value) : null,
            IndoorHumidityPct = src.humidityin,
            SoilMoisturePct = soilMoisture.Count > 0 ? soilMoisture : null,
            SoilTempC = soilTemp.Count > 0 ? soilTemp : null
        };
    }
}
