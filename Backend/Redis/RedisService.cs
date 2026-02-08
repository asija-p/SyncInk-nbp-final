using StackExchange.Redis;
using System.Text.Json;

namespace Backend.Redis
{
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService(string connectionString)
        {
            var redis = ConnectionMultiplexer.Connect(connectionString);
            _db = redis.GetDatabase();
        }

        //strokes
        public async Task SaveStroke(Stroke stroke, string room)
        {
            await _db.ListRightPushAsync($"room:{room}:strokes", JsonSerializer.Serialize(stroke));
        }



        public async Task ClearRoom(string room)
        {
            var server = _db.Multiplexer.GetServer("localhost", 6379);

            foreach (var key in server.Keys(pattern: $"room:{room}:*"))
            {
                await _db.KeyDeleteAsync(key);
            }
        }


        public async Task<List<string>> GetAllStrokes(string room)
        {
            string redisKey = $"room:{room}:strokes";
            var values = await _db.ListRangeAsync(redisKey, 0, -1);
            return values.Select(v => v.ToString()).ToList();
        }
        public async Task AddPointsToCurrentStroke(string roomId, string userId, string strokeId, Position[] points)
        {
            foreach (var point in points)
            {
                await _db.ListRightPushAsync(
                    $"room:{roomId}:currentStroke:{strokeId}",
                    JsonSerializer.Serialize(point)
                );
            }
        }
        public async Task CompleteStroke(string roomId, string userId, string strokeId, Stroke stroke)
        {
            await _db.ListRightPushAsync($"room:{roomId}:strokes", JsonSerializer.Serialize(stroke));

            var undoRef = new StrokeRef { Id = strokeId, UserId = userId };
            await _db.ListRightPushAsync($"room:{roomId}:user:{userId}:undo", JsonSerializer.Serialize(undoRef));

            await _db.KeyDeleteAsync($"room:{roomId}:user:{userId}:redo");

            await _db.KeyDeleteAsync($"room:{roomId}:currentStroke:{strokeId}");
        }

        //rooms
        public async Task AddUserToRoom(string roomId, Guid userId, string username)
        {
            var userIdStr = userId.ToString();

            await _db.SetAddAsync($"room:{roomId}:users", userIdStr);
            await _db.StringSetAsync($"user:{userId}:room", roomId);

            await _db.HashSetAsync($"user:{userId}", new HashEntry[]
            {
                new("username", username)
            });
        }

        public async Task RemoveUserFromRoom(string roomId, Guid userId)
        {
            var userIdStr = userId.ToString();

            await _db.SetRemoveAsync($"room:{roomId}:users", userIdStr);
            await _db.KeyDeleteAsync($"user:{userId}:room");
            await _db.KeyDeleteAsync($"user:{userId}");
            await _db.HashDeleteAsync(
                $"room:{roomId}:drawing",
                userId.ToString()
            );
        }

        public async Task<List<string>> GetUsersInRoom(string roomId)
        {
            var userIds = await _db.SetMembersAsync($"room:{roomId}:users");

            var result = new List<string>();

            foreach (var id in userIds)
            {
                var username = await _db.HashGetAsync($"user:{id}", "username");
                result.Add(username!);
            }

            return result;
        }

        public async Task<string?> GetRoomByUser(Guid userId)
        {
            var userIdStr = userId.ToString();

            var value = await _db.StringGetAsync($"user:{userIdStr}:room");

            if (value.IsNullOrEmpty)
                return null;
            return value;
        }

        public async Task SetRoomStrokesExpire(string roomId, TimeSpan ttl)
        {
            await _db.KeyExpireAsync($"room:{roomId}:strokes", ttl);
        }

        // drawing state

        public async Task SetUserDrawing(string roomId, string username, bool isDrawing)
        {
            await _db.HashSetAsync(
                $"room:{roomId}:drawing",
                username,
                isDrawing ? "1" : "0"
            );
        }

        public async Task<Dictionary<string, bool>> GetDrawingState(string roomId)
        {
            var entries = await _db.HashGetAllAsync($"room:{roomId}:drawing");

            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value == "1"
            );
        }

        //undo redo :((((

        public async Task<Stroke?> Undo(string roomId, string userId)
        {
            var refStr = await _db.ListRightPopAsync($"room:{roomId}:user:{userId}:undo");
            if (refStr.IsNullOrEmpty) return null;

            var undoRef = JsonSerializer.Deserialize<StrokeRef>(refStr)!;

            await _db.ListRightPushAsync($"room:{roomId}:user:{userId}:redo", refStr);

            var roomStrokes = await _db.ListRangeAsync($"room:{roomId}:strokes", 0, -1);
            for (long i = roomStrokes.Length - 1; i >= 0; i--)
            {
                var st = JsonSerializer.Deserialize<Stroke>(roomStrokes[i]);
                if (st!.Id == undoRef.Id && st.UserId == undoRef.UserId)
                {
                    st.Visible = false;
                    await _db.ListSetByIndexAsync($"room:{roomId}:strokes", i, JsonSerializer.Serialize(st));
                    return st;
                }
            }

            return null;
        }

        public async Task<Stroke?> Redo(string roomId, string userId)
        {
            var refStr = await _db.ListRightPopAsync($"room:{roomId}:user:{userId}:redo");
            if (refStr.IsNullOrEmpty) return null;

            var redoRef = JsonSerializer.Deserialize<StrokeRef>(refStr)!;

            await _db.ListRightPushAsync($"room:{roomId}:user:{userId}:undo", refStr);

            var roomStrokes = await _db.ListRangeAsync($"room:{roomId}:strokes", 0, -1);
            for (long i = 0; i < roomStrokes.Length; i++)
            {
                var st = JsonSerializer.Deserialize<Stroke>(roomStrokes[i]);
                if (st!.Id == redoRef.Id && st.UserId == redoRef.UserId)
                {
                    st.Visible = true;
                    await _db.ListSetByIndexAsync($"room:{roomId}:strokes", i, JsonSerializer.Serialize(st));
                    return st;
                }
            }

            return null;
        }

        // replay

        private string ReplayKey(string room) => $"replay:{room}";

        public Task ClearReplay(string room)
        {
            return _db.KeyDeleteAsync(ReplayKey(room));
        }

        public async Task PushReplayStroke(string room, Stroke stroke, Guid userId)
        {

            var json = JsonSerializer.Serialize(stroke);
            await _db.ListRightPushAsync(ReplayKey(room), json);

            var userIdStr = userId.ToString();
            await _db.StringSetAsync($"user:{userIdStr}:room", room);


            await _db.SetAddAsync($"room:{room}:users", userIdStr);
            await _db.HashSetAsync($"user:{userIdStr}", new HashEntry[]
            {
        new("username", "ReplayUser")
            });
        }



        public Task SetReplayExpire(string room, TimeSpan ttl)
        {
            return _db.KeyExpireAsync(ReplayKey(room), ttl);
        }


        public async Task<List<Stroke>> GetReplayStrokes(string room)
        {
            var values = await _db.ListRangeAsync(ReplayKey(room));

            return values
                .Select(v => JsonSerializer.Deserialize<Stroke>(v!)!)
                .ToList();
        }

    }
}