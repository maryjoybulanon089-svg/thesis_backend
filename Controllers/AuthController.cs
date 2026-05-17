using Microsoft.AspNetCore.Mvc;
using ThesisRepository.DTOs;
using ThesisRepository.Services;
using System.ComponentModel.DataAnnotations;

namespace ThesisRepository.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signin")]
        public async Task<ActionResult<AuthResponseDto>> SignIn([FromBody] LoginDto request)
        {
            try
            {
                var response = await _authService.SignIn(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponseDto>> SignUp([FromBody] SignUpDto request)
        {
            try
            {
                var response = await _authService.SignUp(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("signout")]
        public ActionResult SignOutUser()
        {
            // JWT tokens are stateless, so signout is handled client-side
            return Ok(new { message = "Signed out successfully" });
        }

        [HttpGet("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromQuery] string email)
        {
            try
            {
                await _authService.VerifyEmailExists(email);
                return Ok(new { message = "Email exists. Proceed to reset password." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                await _authService.ResetPassword(request.Email, request.NewPassword);
                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class ResetPasswordDto
    {
        public string? RequestId { get; set; } // Kept as optional purely so frontend doesn't break if it sends one
        
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
