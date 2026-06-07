using Microsoft.EntityFrameworkCore;

using DataTrackerApi.Infrastructure.Persistence;
using DataTrackerApi.Features.Users.Models;

namespace DataTrackerApi.Features.Users.Services;
public class UserService
{
    private readonly AppDbContext _context;

    public UserService( AppDbContext context )
    {
        _context = context;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Set<User>()
                             .AsNoTracking()
                             .ToListAsync();
    }

    public async Task<User?> GetByIdAsync( Guid guid )
    {
        var user = await _context.Set<User>()
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync( u => u.PublicId == guid );
        return user;
    }

    // public async Task<User> CreateAsync(User dto)
    // {
    //     var user = new User
    //     {
    //         Username = dto.Username,
    //         Email = dto.Email,
    //         CreatedAt = DateTime.UtcNow
    //     };

    //     _context.Users.Add(user);
    //     await _context.SaveChangesAsync(); // 這時候才會真正對 DB 執行 INSERT

    //     return new UserResponseDto
    //     {
    //         Id = user.Id,
    //         Username = user.Username,
    //         Email = user.Email
    //     };
    // }

    // public async Task<bool> UpdateAsync(int id, UserCreateDto dto)
    // {
    //     var user = await _context.Users.FindAsync(id);
    //     if (user == null) return false;

    //     user.Username = dto.Username;
    //     user.Email = dto.Email;

    //     // EF Core 會自動追蹤實體的變化，直接 SaveChanges 即可
    //     await _context.SaveChangesAsync();
    //     return true;
    // }

    // public async Task<bool> DeleteAsync(int id)
    // {
    //     var user = await _context.Users.FindAsync(id);
    //     if (user == null) return false;

    //     _context.Users.Remove(user);
    //     await _context.SaveChangesAsync();
    //     return true;
    // }
}