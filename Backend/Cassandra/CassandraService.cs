using Cassandra;
using System.Text.Json;
using Backend.Cassandra.Models;
using Backend.Models.Entities;

namespace Backend.Cassandra;

public class CassandraService
{
    private readonly global::Cassandra.ISession _session;

    private readonly PreparedStatement _insertStmt;

    private readonly PreparedStatement _selectSnapshotStmt;

    private readonly PreparedStatement _insertSnapshotMetaStmt;
    private readonly PreparedStatement _selectUserSnapshotsStmt;


    public CassandraService(string[] contactPoints, string keyspace)
    {
        var cluster = Cluster.Builder()
            .AddContactPoints(contactPoints)
            .Build();

        _session = cluster.Connect(keyspace);

        _insertStmt = _session.Prepare(@"
                INSERT INTO strokes_by_snapshot (
                    user_id,
                    room_name,
                    save_id,
                    stroke_id,
                    stroke_date,
                    points_json,
                    color,
                    size,
                    saved_at,
                    visible
                )
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                USING TTL ?
            ");

        _selectSnapshotStmt = _session.Prepare(@"
                SELECT stroke_id, stroke_date, points_json, color, size
                FROM strokes_by_snapshot
                WHERE user_id = ? AND room_name = ? AND save_id = ?
            ");

        _insertSnapshotMetaStmt = _session.Prepare(@"
            INSERT INTO latest_snapshot_by_user (
                user_id,
                room_name,
                saved_at,
                save_id
            ) VALUES (?, ?, ?, ?)
        ");

        _selectUserSnapshotsStmt = _session.Prepare(@"
            SELECT room_name, save_id, saved_at
            FROM latest_snapshot_by_user
            WHERE user_id = ?
        ");
    }

    public async Task<List<SnapshotMetadata>> GetUserSnapshotsAsync(Guid userId)
    {
        var bound = _selectUserSnapshotsStmt.Bind(userId);
        var rs = await _session.ExecuteAsync(bound);

        var result = new List<SnapshotMetadata>();

        foreach (var row in rs)
        {
            result.Add(new SnapshotMetadata
            {
                UserId = userId,
                RoomName = row.GetValue<string>("room_name"),
                SaveId = row.GetValue<Guid>("save_id"),
                SavedAt = row.GetValue<DateTime>("saved_at")
            });
        }

        return result;
    }


    public async Task SaveSnapshotMetadataAsync(
        Guid userId,
        string roomName,
        Guid saveId,
        DateTime savedAt
    )
    {
        var bound = _insertSnapshotMetaStmt.Bind(
            userId,
            roomName,
            savedAt,
            saveId
        );

        await _session.ExecuteAsync(bound);
    }

    public async Task SaveStrokesSnapshotAsync(Guid userId, string roomName, Guid saveId, List<string> redisStrokes)
    {
        var savedAt = DateTime.UtcNow;
        var batch = new BatchStatement();

        foreach (var strokeJson in redisStrokes)
        {
            var stroke = JsonSerializer.Deserialize<Stroke>(strokeJson);
            if (stroke == null) continue;


            if (!stroke.Visible)
                continue;

            var bound = _insertStmt.Bind(
                userId,
                roomName,
                saveId,
                Guid.NewGuid(),               // stroke_id
                stroke.StrokeDate,
                JsonSerializer.Serialize(stroke.Points),
                stroke.Color,
                stroke.Size,
                savedAt,
                stroke.Visible,
                7 * 24 * 3600                 // TTL 1 week
            );

            batch.Add(bound);
        }

        await _session.ExecuteAsync(batch);
    }

    public async Task<List<Stroke>> GetStrokesForSnapshotAsync(Guid userId, string roomName, Guid saveId)
    {
        var bound = _selectSnapshotStmt.Bind(userId, roomName, saveId);
        var rs = await _session.ExecuteAsync(bound);

        var strokes = new List<Stroke>();

        foreach (var row in rs)
        {
            var points = JsonSerializer.Deserialize<List<Position>>(row.GetValue<string>("points_json")) ?? new List<Position>();

            strokes.Add(new Stroke
            {
                Points = points.ToArray(),
                Color = row.GetValue<string>("color"),
                Size = Convert.ToInt32(row.GetValue<double>("size")),
                StrokeDate = row.GetValue<DateTime>("stroke_date")
            });

        }

        return strokes;
    }

    public async Task<List<StrokeEntity>> GetSnapshotStrokesAsync(Guid userId, string roomName, Guid saveId)
    {
        var selectStmt = _session.Prepare(@"
        SELECT user_id, room_name, save_id, stroke_id, color, points_json, saved_at, size, stroke_date,visible
        FROM strokes_by_snapshot 
        WHERE user_id = ? AND room_name = ? AND save_id = ?");

        var bound = selectStmt.Bind(userId, roomName, saveId);
        var rowSet = await _session.ExecuteAsync(bound);

        var list = new List<StrokeEntity>();
        foreach (var row in rowSet)
        {
            list.Add(new StrokeEntity
            {
                UserId = row.GetValue<Guid>("user_id"),
                RoomName = row.GetValue<string>("room_name"),
                SaveId = row.GetValue<Guid>("save_id"),
                StrokeId = row.GetValue<Guid>("stroke_id"),
                Color = row.GetValue<string>("color"),
                PointsJson = row.GetValue<string>("points_json"),
                Size = row.GetValue<double>("size"),
                StrokeDate = row.GetValue<DateTime>("stroke_date"),
                SavedAt = row.GetValue<DateTime>("saved_at"),
                Visible = row.GetValue<bool>("visible")
            });
        }
        return list;
    }



    //Activity functionality

    public async Task IncrementCounterAsync(
    string roomName,
    DateTime timestamp,
    string counterColumn
)
    {
        var bucket = GetMinuteBucket(timestamp);

        var cql = $@"
        UPDATE drawing_activity_counters
        SET {counterColumn} = {counterColumn} + 1
        WHERE room_name = ? AND minute_bucket = ?";

        var stmt = _session.Prepare(cql);
        var bound = stmt.Bind(roomName, bucket);

        await _session.ExecuteAsync(bound);
    }

    public async Task MarkUserActiveAsync(string roomName, Guid userId, DateTime timestamp)
    {
        var bucket = GetMinuteBucket(timestamp); // timestamp in UTC

        var cql = @"
        UPDATE drawing_activity_state
        SET active_users = active_users + ?
        WHERE room_name = ? AND minute_bucket = ?";

        var stmt = _session.Prepare(cql);

        var activeUsers = new HashSet<Guid> { userId };

        var bound = stmt.Bind(activeUsers, roomName, bucket); // bind Guid set, string, DateTime
        await _session.ExecuteAsync(bound);
    }


    private static DateTime GetMinuteBucket(DateTime utcNow)
    {
        return new DateTime(
            utcNow.Year,
            utcNow.Month,
            utcNow.Day,
            utcNow.Hour,
            utcNow.Minute,
            0,
            DateTimeKind.Utc
        );
    }



    // Timeline counter row
    public class DrawingActivityCounter
    {
        public DateTime MinuteBucket { get; set; }
        public long StrokesCompleted { get; set; }
        public long Undos { get; set; }
        public long Redos { get; set; }
    }

    public class DrawingActivityState
    {
        public DateTime MinuteBucket { get; set; }
        public List<Guid> ActiveUsers { get; set; } = new();
    }

    public async Task<List<DrawingActivityCounter>> GetActivityCountersAsync(string roomName)
    {
        var stmt = _session.Prepare(@"
        SELECT minute_bucket, strokes_completed, undos, redos
        FROM drawing_activity_counters
        WHERE room_name = ?");

        var bound = stmt.Bind(roomName);
        var rows = await _session.ExecuteAsync(bound);

        var result = new List<DrawingActivityCounter>();
        foreach (var row in rows)
        {
            result.Add(new DrawingActivityCounter
            {
                MinuteBucket = row.GetValue<DateTime>("minute_bucket"),
                StrokesCompleted = row.GetValue<long?>("strokes_completed") ?? 0,
                Undos = row.GetValue<long?>("undos") ?? 0,
                Redos = row.GetValue<long?>("redos") ?? 0
            });
        }
        return result;
    }

    public async Task<List<DrawingActivityState>> GetActiveUsersAsync(string roomName)
    {
        var stmt = _session.Prepare(@"
        SELECT minute_bucket, active_users
        FROM drawing_activity_state
        WHERE room_name = ?");

        var bound = stmt.Bind(roomName);
        var rows = await _session.ExecuteAsync(bound);

        var result = new List<DrawingActivityState>();
        foreach (var row in rows)
        {
            var users = row.GetValue<IEnumerable<Guid>>("active_users") ?? Array.Empty<Guid>();
            result.Add(new DrawingActivityState
            {
                MinuteBucket = row.GetValue<DateTime>("minute_bucket"),
                ActiveUsers = users.ToList()
            });
        }
        return result;
    }

}

