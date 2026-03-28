namespace LawncareApi.Models;

/// <summary>
/// Represents the raw payload pushed by the Ecowitt GW1100 gateway via its
/// Custom Server HTTP POST protocol. Field names match the Ecowitt push
/// specification so they are deserialised directly from the form-encoded body.
/// </summary>
public class EcowittReading
{
    // ── Gateway identity ────────────────────────────────────────────────────
    public string? PASSKEY { get; set; }
    public string? stationtype { get; set; }
    public long? runtime { get; set; }
    public string? freq { get; set; }
    public string? model { get; set; }

    // ── Timestamp ───────────────────────────────────────────────────────────
    public string? dateutc { get; set; }

    // ── WN32 – outdoor temperature & humidity ───────────────────────────────
    public double? tempf { get; set; }
    public double? humidity { get; set; }

    // ── Wind (GW1100 internal sensor) ───────────────────────────────────────
    public double? windspeedmph { get; set; }
    public double? windgustmph { get; set; }
    public int? winddir { get; set; }

    // ── Rain ────────────────────────────────────────────────────────────────
    public double? rainratein { get; set; }
    public double? eventrainin { get; set; }
    public double? hourlyrainin { get; set; }
    public double? dailyrainin { get; set; }
    public double? weeklyrainin { get; set; }
    public double? monthlyrainin { get; set; }
    public double? yearlyrainin { get; set; }
    public double? totalrainin { get; set; }

    // ── Barometric pressure ─────────────────────────────────────────────────
    public double? baromrelin { get; set; }
    public double? baromabsin { get; set; }

    // ── UV / solar ──────────────────────────────────────────────────────────
    public double? solarradiation { get; set; }
    public int? uv { get; set; }

    // ── Indoor sensor ───────────────────────────────────────────────────────
    public double? tempinf { get; set; }
    public double? humidityin { get; set; }

    // ── WH51 soil moisture – up to 8 channels ───────────────────────────────
    public int? soilmoisture1 { get; set; }
    public int? soilmoisture2 { get; set; }
    public int? soilmoisture3 { get; set; }
    public int? soilmoisture4 { get; set; }
    public int? soilmoisture5 { get; set; }
    public int? soilmoisture6 { get; set; }
    public int? soilmoisture7 { get; set; }
    public int? soilmoisture8 { get; set; }

    // ── WH51 soil capacitance (raw ADC) ─────────────────────────────────────
    public int? soilad1 { get; set; }
    public int? soilad2 { get; set; }
    public int? soilad3 { get; set; }
    public int? soilad4 { get; set; }
    public int? soilad5 { get; set; }
    public int? soilad6 { get; set; }
    public int? soilad7 { get; set; }
    public int? soilad8 { get; set; }

    // ── Extra temperature sensors ───────────────────────────────────────────
    public double? temp1f { get; set; }
    public double? temp2f { get; set; }
    public double? temp3f { get; set; }
    public double? temp4f { get; set; }
    public double? temp5f { get; set; }
    public double? temp6f { get; set; }
    public double? temp7f { get; set; }
    public double? temp8f { get; set; }

    // ── Extra humidity sensors ──────────────────────────────────────────────
    public int? humidity1 { get; set; }
    public int? humidity2 { get; set; }
    public int? humidity3 { get; set; }
    public int? humidity4 { get; set; }
    public int? humidity5 { get; set; }
    public int? humidity6 { get; set; }
    public int? humidity7 { get; set; }
    public int? humidity8 { get; set; }
}
