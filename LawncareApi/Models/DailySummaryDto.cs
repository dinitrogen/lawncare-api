namespace LawncareApi.Models;

public class DailySummaryDto
{
    public string Date { get; set; } = "";
    public double HighTempC { get; set; }
    public double LowTempC { get; set; }
    public double AvgHumidityPct { get; set; }
    public double? AvgSoilMoisturePct { get; set; }
    public double? AvgSoilTempC { get; set; }
}
