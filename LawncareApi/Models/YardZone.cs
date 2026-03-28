using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class FilledCell
{
    [FirestoreProperty]
    public int X { get; set; }

    [FirestoreProperty]
    public int Y { get; set; }
}

[FirestoreData]
public class ZoneLabel
{
    [FirestoreProperty]
    public double X { get; set; }

    [FirestoreProperty]
    public double Y { get; set; }

    [FirestoreProperty]
    public string Text { get; set; } = string.Empty;
}

[FirestoreData]
public class ZoneSketch
{
    [FirestoreProperty]
    public int GridSizeX { get; set; }

    [FirestoreProperty]
    public int GridSizeY { get; set; }

    [FirestoreProperty]
    public int CellSizeFt { get; set; }

    [FirestoreProperty]
    public IList<FilledCell>? FilledCells { get; set; }

    [FirestoreProperty]
    public IList<ZoneLabel>? Labels { get; set; }
}

[FirestoreData]
public class YardZone
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public double Area { get; set; }

    [FirestoreProperty]
    public string? GrassType { get; set; }

    [FirestoreProperty]
    public string? SoilType { get; set; }

    [FirestoreProperty]
    public string? SunExposure { get; set; }

    [FirestoreProperty]
    public ZoneSketch? SketchData { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class YardZoneRequest
{
    public string Name { get; set; } = string.Empty;
    public double Area { get; set; }
    public string? GrassType { get; set; }
    public string? SoilType { get; set; }
    public string? SunExposure { get; set; }
    public ZoneSketch? SketchData { get; set; }
    public string? Notes { get; set; }
}
