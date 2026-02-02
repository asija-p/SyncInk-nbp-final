using Backend.Data;
using Microsoft.EntityFrameworkCore;

public class SavedPictureService
{
    private readonly AppDbContext _db;

    public SavedPictureService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsAsync(Guid userId, string roomName, Guid saveId)
    {
        return await _db.savedPictures.AnyAsync(sp =>
            sp.UserId == userId &&
            sp.RoomName == roomName &&
            sp.SaveId == saveId
        );
    }
}
