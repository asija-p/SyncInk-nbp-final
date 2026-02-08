using Backend.Cassandra;
using Backend.Cassandra.Models;
using Backend.Redis;
using Microsoft.AspNetCore.SignalR;
using SyncInk.Hubs;
using System.Text.Json;

namespace Backend.Services;

public class ReplayService
{
    private readonly CassandraService _cassandra;
    private readonly RedisService _redis;
    private readonly IHubContext<SyncInkHub> _hubContext;



    public ReplayService(
        CassandraService cassandra,
        RedisService redis,
        IHubContext<SyncInkHub> hubContext)
    {
        _cassandra = cassandra;
        _redis = redis;
        _hubContext = hubContext;
    }


    public async Task<int> StartReplayAsync(Guid userId, string roomName, Guid saveId)
    {
        List<StrokeEntity> entities = await _cassandra.GetSnapshotStrokesAsync(userId, roomName, saveId);

        var sortedEntities = entities.OrderBy(e => e.StrokeDate).ToList();

        var replayStrokes = new List<Stroke>();

        foreach (var entity in sortedEntities)
        {
            var stroke = new Stroke
            {
                Id = entity.StrokeId.ToString(),
                Points = JsonSerializer.Deserialize<Position[]>(entity.PointsJson),
                Color = entity.Color,
                Size = entity.Size,
                StrokeDate = entity.StrokeDate
            };

            await _redis.PushReplayStroke(roomName, stroke, userId);
            replayStrokes.Add(stroke);

            await Task.Delay(100); // optional delay to simulate replay timing
        }

        // Send all strokes at once to frontend
        await _hubContext.Clients.Group(roomName).SendAsync("ReceiveReplayData", replayStrokes);

        return sortedEntities.Count;
    }

}