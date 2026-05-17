using Microsoft.EntityFrameworkCore;
using ThesisRepository.Data;
using ThesisRepository.Models;

namespace ThesisRepository.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ApplicationDbContext _context;

        public PasswordResetService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetRequest> CreateRequest(string email)
        {
            // Check if there's already a pending request
            var existingRequest = await _context.PasswordResetRequests
                .FirstOrDefaultAsync(r => r.Email == email && r.Status == "pending");

            if (existingRequest != null)
            {
                throw new Exception("A password reset request is already pending for this email");
            }

            var request = new PasswordResetRequest
            {
                Email = email,
                Status = "pending",
                RequestedAt = DateTime.UtcNow
            };

            _context.PasswordResetRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<List<PasswordResetRequest>> GetAllRequests()
        {
            return await _context.PasswordResetRequests
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<PasswordResetRequest> UpdateRequest(string id, string status, string processedBy)
        {
            var request = await _context.PasswordResetRequests.FindAsync(id);
            
            if (request == null)
                throw new Exception("Password reset request not found");

            request.Status = status;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = processedBy;

            await _context.SaveChangesAsync();

            return request;
        }

        public async Task DeleteRequest(string id)
        {
            var request = await _context.PasswordResetRequests.FindAsync(id);
            
            if (request == null)
                throw new Exception("Password reset request not found");

            _context.PasswordResetRequests.Remove(request);
            await _context.SaveChangesAsync();
        }
    }
}
