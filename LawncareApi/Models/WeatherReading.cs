using Google.Cloud.Firestore;

namespace LawncareApi.Models;

/// <summary>
/// Normalised weather reading stored in Firestore and served to clients.
/// All temperature values are stored in Celsius; wind speed in km/h;
/// pressure in hPa; rain in mm.
/// </summary>
[FirestoreData]
public class WeatherReading
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public DateTime Timestamp { get; set; }

    // ── WN32 outdoor ────────────────────────────────────────────────────────
    [FirestoreProperty]
    public double? OutdoorTempC { get; set; }

    [FirestoreProperty]
    public double? OutdoorHumidityPct { get; set; }

    // ── Wind ────────────────────────────────────────────────────────────────
    [FirestoreProperty]
    public double? WindSpeedKmh { get; set; }

    [FirestoreProperty]
    public double? WindGustKmh { get; set; }

    [FirestoreProperty]
    public int? WindDirectionDeg { get; set; }

    // ── Rain ────────────────────────────────────────────────────────────────
    [FirestoreProperty]
    public double? RainRateMmh { get; set; }

    [FirestoreProperty]
    public double? DailyRainMm { get; set; }

    [FirestoreProperty]
    public double? WeeklyRainMm { get; set; }

    [FirestoreProperty]
    public double? MonthlyRainMm { get; set; }

    // ── Pressure ────────────────────────────────────────────────────────────
    [FirestoreProperty]
    public double? PressureRelHpa { get; set; }

    [FirestoreProperty]
    public double? PressureAbsHpa { get; set; }

    // ── UV / solar ──────────────────────────────────────────────────────────
    [FirestoreProperty]
    public double? SolarRadiationWm2 { get; set; }

    [FirestoreProperty]
    public int? UvIndex { get; set; }

    // ── Indoor (GW1100 built-in) ─────────────────────────────────────────────
    [FirestoreProperty]
    public double? IndoorTempC { get; set; }

    [FirestoreProperty]
    public double? IndoorHumidityPct { get; set; }

    // ── WH51 soil moisture channels (% volumetric water content) ────────────
    [FirestoreProperty]
    public IList<int>? SoilMoisturePct { get; set; }

    // ── WN34 soil temperature channels (°C) ─────────────────────────────────
    [FirestoreProperty]
    public IList<double>? SoilTempC { get; set; }

    // ── Source device info ──────────────────────────────────────────────────
    [FirestoreProperty]
    public string? StationType { get; set; }

    // ── Computed helpers (not stored) ────────────────────────────────────────
    public double? FeelsLikeC => OutdoorTempC.HasValue && WindSpeedKmh.HasValue
        ? Math.Round(
            13.12 + 0.6215 * OutdoorTempC.Value
            - 11.37 * Math.Pow(WindSpeedKmh.Value, 0.16)
            + 0.3965 * OutdoorTempC.Value * Math.Pow(WindSpeedKmh.Value, 0.16), 1)
        : OutdoorTempC;
}
