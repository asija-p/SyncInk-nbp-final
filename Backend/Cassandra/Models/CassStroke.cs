namespace Backend.Cassandra.Models;

public class StrokeEntity
{
    public Guid UserId { get; set; }
    public string RoomName { get; set; } = null!;
    public Guid SaveId { get; set; }
    public Guid StrokeId { get; set; }
    public string PointsJson { get; set; } = null!;
    public string Color { get; set; } = null!;
    public double Size { get; set; }

    public DateTime StrokeDate { get; set; }
    public DateTime SavedAt { get; set; }
    public bool Visible { get; set; } = true;
}

public class SnapshotMetadata
{
    public Guid UserId { get; set; }
    public string RoomName { get; set; } = null!;
    public Guid SaveId { get; set; }
    public DateTime SavedAt { get; set; }
}

public class DrawingActivityMinute
{
    public string RoomName { get; set; }
    public DateTime MinuteBucket { get; set; }

    public long StrokesCompleted { get; set; }
    public long Undos { get; set; }
    public long Redos { get; set; }

    public HashSet<Guid> ActiveUsers { get; set; }
}
