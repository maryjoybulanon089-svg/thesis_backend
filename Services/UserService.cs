using Microsoft.EntityFrameworkCore;
using ThesisRepository.Data;
using ThesisRepository.DTOs;

namespace ThesisRepository.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserDto>> GetAllUsers()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return users.Select(MapToDto).ToList();
        }

        public async Task<UserDto?> GetUserById(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            var user = await _context.Users.FindAsync(intId);
            return user == null ? null : MapToDto(user);
        }

        public async Task UpdateUserStatus(string userId, bool? isApproved, bool? isActive)
        {
            if (!int.TryParse(userId, out var intId))
                throw new Exception("Invalid user ID.");

            var user = await _context.Users.FindAsync(intId)
                       ?? throw new Exception("User not found.");

            if (isApproved.HasValue)
                user.IsApproved = isApproved.Value;

            if (isActive.HasValue)
                user.IsActive = isActive.Value;

            await _context.SaveChangesAsync();
        }

        // ── Helper ───────────────────────────────────────────────────────────────

        private static UserDto MapToDto(Models.User u) => new UserDto
        {
            Id         = u.UserId.ToString(),
            Email      = u.Email,
            FirstName  = u.FirstName,
            LastName   = u.LastName,
            Role       = u.Role,
            IsApproved = u.IsApproved,
            IsActive   = u.IsActive,
            Department = u.Department,
            CreatedAt  = u.CreatedAt,
            LastLogin  = u.LastLogin
        };
    }
}
