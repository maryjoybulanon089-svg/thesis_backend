using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ThesisRepository.Models;
using ThesisRepository.Services;

namespace ThesisRepository.Controllers
{
    [ApiController]
    [Route("api/password-reset")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _passwordResetService;

        public PasswordResetController(IPasswordResetService passwordResetService)
        {
            _passwordResetService = passwordResetService;
        }

        [HttpPost]
        public async Task<ActionResult<PasswordResetRequest>> CreateRequest([FromBody] CreateResetDto request)
        {
            try
            {
                var resetRequest = await _passwordResetService.CreateRequest(request.Email);
                return Ok(resetRequest);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<PasswordResetRequest>>> GetAllRequests()
        {
            try
            {
                var requests = await _passwordResetService.GetAllRequests();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<PasswordResetRequest>> UpdateRequest(
            string id,
            [FromBody] UpdateResetDto request)
        {
            try
            {
                var updatedRequest = await _passwordResetService.UpdateRequest(
                    id, 
                    request.Status, 
                    request.ProcessedBy);
                return Ok(updatedRequest);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteRequest(string id)
        {
            try
            {
                await _passwordResetService.DeleteRequest(id);
                return Ok(new { message = "Password reset request deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CreateResetDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateResetDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
        public string ProcessedBy { get; set; } = string.Empty;
    }
}
