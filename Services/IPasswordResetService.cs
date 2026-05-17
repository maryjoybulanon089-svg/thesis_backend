using ThesisRepository.Models;

namespace ThesisRepository.Services
{
    public interface IPasswordResetService
    {
        Task<PasswordResetRequest> CreateRequest(string email);
        Task<List<PasswordResetRequest>> GetAllRequests();
        Task<PasswordResetRequest> UpdateRequest(string id, string status, string processedBy);
        Task DeleteRequest(string id);
    }
}
