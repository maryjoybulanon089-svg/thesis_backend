using ThesisRepository.DTOs;
using ThesisRepository.Controllers;

namespace ThesisRepository.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> SignIn(LoginDto request);
        Task<AuthResponseDto> SignUp(SignUpDto request);
        string GenerateJwtToken(string userId, string email, string role);
        Task<bool> VerifyEmailExists(string email);
        Task ResetPassword(string email, string newPassword);
    }
}
