using System.Security.Claims;
using System.Text.Json;
using Backend.Redis;
using Microsoft.AspNetCore.SignalR;

namespace SyncInk.Hubs
{
    public class SyncInkHub : Hub
    {
        private readonly RedisService _redis;

        public SyncInkHub(RedisService redis)
        {
            _redis = redis;
        }

        public async Task SendStroke(Stroke stroke, string strokeId)
        {
            if (stroke == null) return;

            var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var room = await _redis.GetRoomByUser(userId);
            if (room == null) return;

            await Clients.OthersInGroup(room)
                .SendAsync("ReceiveStroke", stroke);

            await _redis.AddPointsToCurrentStroke(room, userId.ToString(), strokeId, stroke.Points);
        }

        public async Task CompleteStroke(Stroke stroke, string strokeId)
        {
            var userId = Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var room = await _redis.GetRoomByUser(Guid.Parse(userId));
            if (room == null) return;

            stroke.UserId = userId;
            stroke.Id = strokeId;

            await _redis.CompleteStroke(room, userId, strokeId, stroke);
        }

        //room

        public async Task<string> MakeRoom()
        {
            var userId = Guid.Parse(
                Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var username = Context.User!.FindFirst(ClaimTypes.Name)!.Value;

            string roomName = Guid.NewGuid().ToString("N")[..4];

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            await _redis.AddUserToRoom(roomName, userId, username);

            await _redis.SetRoomStrokesExpire(roomName, TimeSpan.FromHours(24));

            var users = await _redis.GetUsersInRoom(roomName);

            await Clients.Group(roomName)
                .SendAsync("UsersUpdated", users);

            return roomName;
        }

        public async Task<string> JoinRoom(string roomName)
        {
            var userId = Guid.Parse(
                 Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value
             );

            var username = Context.User!.FindFirst(ClaimTypes.Name)!.Value;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            await _redis.AddUserToRoom(roomName, userId, username);

            var users = await _redis.GetUsersInRoom(roomName);

            await Clients.Group(roomName)
                .SendAsync("UsersUpdated", users);

            return roomName;
        }

        public async Task GetHistory(string roomName)
        {
            var history = await _redis.GetAllStrokes(roomName);
            foreach (var item in history)
            {
                var stroke = JsonSerializer.Deserialize<Stroke>(item!);
                await Clients.Caller.SendAsync("ReceiveStroke", stroke);
            }
        }
        public async Task LeaveRoom()
        {
            var userId = Guid.Parse(
                 Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value
             );

            var room = await _redis.GetRoomByUser(userId);
            if (room == null) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);

            await _redis.RemoveUserFromRoom(room, userId);

            var users = await _redis.GetUsersInRoom(room);
            if (users.Count == 0)
            {
                await _redis.ClearRoom(room);
            }
            await Clients.Group(room)
                .SendAsync("UsersUpdated", users);

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context?.User == null)
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            var userIdClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            var userId = Guid.Parse(userIdClaim.Value);

            var room = await _redis.GetRoomByUser(userId);
            if (room == null)
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            await _redis.RemoveUserFromRoom(room, userId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);

            var users = await _redis.GetUsersInRoom(room);
            await Clients.Group(room)
                .SendAsync("UsersUpdated", users);

            await base.OnDisconnectedAsync(exception);
        }

        // drawing state

        public async Task SetDrawing(bool isDrawing)
        {
            var userId = Guid.Parse(
                Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var username = Context.User!.FindFirst(ClaimTypes.Name)!.Value;

            var room = await _redis.GetRoomByUser(userId);
            if (room == null) return;

            await _redis.SetUserDrawing(room, username, isDrawing);

            var states = await _redis.GetDrawingState(room);

            await Clients.Group(room)
                .SendAsync("DrawingStateUpdated", states);
        }

        // undo redo

        public async Task UndoStroke()
        {
            var userId = Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var room = await _redis.GetRoomByUser(Guid.Parse(userId));
            if (room == null) return;

            var stroke = await _redis.Undo(room, userId);
            if (stroke != null)
            {
                await Clients.Group(room).SendAsync("StrokeRemoved", stroke);
            }
        }

        public async Task RedoStroke()
        {
            var userId = Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var room = await _redis.GetRoomByUser(Guid.Parse(userId));
            if (room == null) return;

            var stroke = await _redis.Redo(room, userId);
            if (stroke != null)
            {
                await Clients.Group(room).SendAsync("ReceiveStroke", stroke);
            }
        }

        // replay

        public async Task GetReplayHistory(string roomName)
        {
            var strokes = await _redis.GetReplayStrokes(roomName);

            await Clients.Caller.SendAsync("ReceiveReplayData", strokes);
        }

    }
}