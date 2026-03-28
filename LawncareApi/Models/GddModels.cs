using Google.Cloud.Firestore;

namespace LawncareApi.Models;

public class DailyGddEntry
{
    public string Date { get; set; } = string.Empty;
    public double TempMax { get; set; }
    public double TempMin { get; set; }
    public double Gdd { get; set; }
    public double CumulativeGdd { get; set; }
}

[FirestoreData]
public class GddCacheEntry
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public int Year { get; set; }

    [FirestoreProperty]
    public IList<GddCacheDay>? Data { get; set; }

    [FirestoreProperty]
    public string UpdatedAt { get; set; } = string.Empty;
}

[FirestoreData]
public class GddCacheDay
{
    [FirestoreProperty]
    public string Date { get; set; } = string.Empty;

    [FirestoreProperty]
    public double TempMax { get; set; }

    [FirestoreProperty]
    public double TempMin { get; set; }

    [FirestoreProperty]
    public double Gdd { get; set; }

    [FirestoreProperty]
    public double CumulativeGdd { get; set; }
}

public class OpenMeteoResponse
{
    public OpenMeteoDailyData Daily { get; set; } = new();
}

public class OpenMeteoDailyData
{
    public IList<string> Time { get; set; } = [];
    public IList<double> Temperature_2m_max { get; set; } = [];
    public IList<double> Temperature_2m_min { get; set; } = [];
}
