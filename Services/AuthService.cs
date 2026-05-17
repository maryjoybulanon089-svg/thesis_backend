using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ThesisRepository.Data;
using ThesisRepository.DTOs;
using ThesisRepository.Models;
using ThesisRepository.Controllers;

namespace ThesisRepository.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ── Sign In ──────────────────────────────────────────────────────────────
        public async Task<AuthResponseDto> SignIn(LoginDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                throw new Exception("Invalid email or password");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid email or password");

            if (!user.IsActive)
                throw new Exception("Your account has been deactivated. Please contact the administrator.");

            if (!user.IsApproved)
                throw new Exception("Your account is pending administrator approval.");

            // Update last-login timestamp
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user.UserId.ToString(), user.Email, user.Role);

            return new AuthResponseDto
            {
                Token = token,
                User  = MapToDto(user)
            };
        }

        // ── Sign Up ──────────────────────────────────────────────────────────────
        public async Task<AuthResponseDto> SignUp(SignUpDto request)
        {
            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existing != null)
                throw new Exception("An account with this email already exists.");

            var (firstName, lastName) = NormalizeNames(request);

            var user = new User
            {
                Email        = request.Email,
                FirstName    = firstName,
                LastName     = lastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role         = request.Role,
                Department   = request.Department,
                IsApproved   = false,  // requires admin approval
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user.UserId.ToString(), user.Email, user.Role);

            return new AuthResponseDto
            {
                Token = token,
                User  = MapToDto(user)
            };
        }

        // ── JWT ──────────────────────────────────────────────────────────────────
        public string GenerateJwtToken(string userId, string email, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(
                _configuration["JwtSettings:Secret"] ??
                "YourSuperSecretKeyForThesisRepositorySystem2024!@#$%");

            // Admin users get shorter session timeout, others get standard expiry
            DateTime expiresAt;
            if (role == "admin")
            {
                var adminSessionMinutes = int.TryParse(_configuration["JwtSettings:AdminSessionTimeoutMinutes"], out var minutes) ? minutes : 30;
                expiresAt = DateTime.UtcNow.AddMinutes(adminSessionMinutes);
            }
            else
            {
                var expiryInDays = int.TryParse(_configuration["JwtSettings:ExpiryInDays"], out var days) ? days : 7;
                expiresAt = DateTime.UtcNow.AddDays(expiryInDays);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // ── Helper ───────────────────────────────────────────────────────────────
        private static (string FirstName, string LastName) NormalizeNames(SignUpDto request)
        {
            var firstName = request.FirstName?.Trim() ?? string.Empty;
            var lastName = request.LastName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                var combined = request.Name ?? request.FullName;
                if (!string.IsNullOrWhiteSpace(combined))
                {
                    var parts = combined.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && string.IsNullOrWhiteSpace(firstName))
                    {
                        firstName = parts[0];
                    }
                    if (parts.Length > 1 && string.IsNullOrWhiteSpace(lastName))
                    {
                        lastName = string.Join(' ', parts.Skip(1));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                throw new Exception("First name and last name are required.");
            }

            return (firstName, lastName);
        }

        private static UserDto MapToDto(User u) => new UserDto
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

        // ── Verify Email ─────────────────────────────────────────────────────────
        public async Task<bool> VerifyEmailExists(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                throw new Exception("No account found with this email address.");
            }
            return true;
        }

        // ── Reset Password ───────────────────────────────────────────────────────
        public async Task ResetPassword(string email, string newPassword)
        {
            // Find the user directly
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found.");

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            // Optionally, we could clean up any old requests left over in the DB
            var oldRequests = await _context.PasswordResetRequests.Where(r => r.Email == email).ToListAsync();
            if (oldRequests.Any())
            {
                _context.PasswordResetRequests.RemoveRange(oldRequests);
            }
            
            await _context.SaveChangesAsync();
        }
    }
}
