using ThesisRepository.DTOs;
using ThesisRepository.Models;

namespace ThesisRepository.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsers();
        Task<UserDto?> GetUserById(string id);
        Task UpdateUserStatus(string userId, bool? isApproved, bool? isActive);
    }
}
