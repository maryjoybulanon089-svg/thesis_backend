using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ThesisRepository.Data;
using ThesisRepository.DTOs;
using ThesisRepository.Models;

namespace ThesisRepository.Services
{
    public class ThesisService : IThesisService
    {
        private readonly ApplicationDbContext _context;

        public ThesisService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Read ─────────────────────────────────────────────────────────────────

        public async Task<List<ThesisDto>> GetAllTheses()
        {
            var theses = await _context.Theses
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            return theses.Where(IsPdfAvailable).Select(MapToDto).ToList();
        }

        public async Task<ThesisDto?> GetThesisById(string id)
        {
            if (!int.TryParse(id, out var intId))
                return null;

            var thesis = await _context.Theses
                .FirstOrDefaultAsync(t => t.ThesisId == intId && !t.IsDeleted);
                
            if (thesis == null || !IsPdfAvailable(thesis))
                return null;

            return MapToDto(thesis);
        }

        public async Task<List<ThesisDto>> SearchTheses(string? query, string? department, string? fieldOfResearch, int? year, string? status = "approved", string? researchType = null)
        {
            var thesesQuery = _context.Theses.Where(t => !t.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                thesesQuery = thesesQuery.Where(t => t.Status == status);

            if (!string.IsNullOrEmpty(researchType))
                thesesQuery = thesesQuery.Where(t => t.ResearchType == researchType);

            if (!string.IsNullOrEmpty(department))
                thesesQuery = thesesQuery.Where(t => t.Department == department);

            if (!string.IsNullOrEmpty(fieldOfResearch))
                thesesQuery = thesesQuery.Where(t => t.FieldOfResearch == fieldOfResearch);

            if (year.HasValue)
                thesesQuery = thesesQuery.Where(t => t.Year == year.Value);

            if (!string.IsNullOrEmpty(query))
            {
                var lowerQuery = query.ToLower();
                thesesQuery = thesesQuery.Where(t => 
                    t.Title.ToLower().Contains(lowerQuery) || 
                    t.Abstract.ToLower().Contains(lowerQuery) || 
                    t.Keywords.ToLower().Contains(lowerQuery) ||
                    t.Authors.ToLower().Contains(lowerQuery));
            }

            var theses = await thesesQuery
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            return theses.Where(IsPdfAvailable).Select(MapToDto).ToList();
        }

        // ── Write ────────────────────────────────────────────────────────────────

        public async Task<ThesisDto> CreateThesis(CreateThesisDto request)
        {
            // Parse uploader's UserId string → int
            int? uploadedById = int.TryParse(request.UploadedBy, out var uid) ? uid : (int?)null;

            // Normalize DOI before persisting
            var normalizedDoi = NormalizeDoi(request.Doi);

            var thesis = new Thesis
            {
                Title           = request.Title,
                Abstract        = request.Abstract,
                Keywords        = JsonSerializer.Serialize(request.Keywords ?? Array.Empty<string>()),
                Authors         = request.Authors,
                Advisors        = request.Advisors,
                Department      = request.Department,
                FieldOfResearch = request.FieldOfResearch,
                Year            = request.Year,
                FilePath        = request.PdfUrl,   // PdfUrl → FilePath column
                Status          = "pending",
                UploadedBy      = uploadedById,
                UploadedAt      = DateTime.UtcNow,
                MainAuthorEmail = request.MainAuthorEmail,
                CoAuthorEmail   = request.CoAuthorEmail,
                ResearchType    = request.ResearchType,
                Doi             = normalizedDoi
            };

            // If PDF data supplied in request (base64 or data URL), store in DB
            if (!string.IsNullOrWhiteSpace(request.PdfData))
            {
                var base64 = request.PdfData;
                var comma = base64.IndexOf(',');
                if (comma >= 0)
                    base64 = base64[(comma + 1)..];

                try
                {
                    var bytes = Convert.FromBase64String(base64);

                    // Create UploadedFile record and reference it
                    var uploaded = new Models.UploadedFile
                    {
                        Id = Guid.NewGuid().ToString(),
                        FileName = request.PdfUrl ?? $"thesis_{DateTime.UtcNow.Ticks}.pdf",
                        FileType = "application/pdf",
                        UploadedAt = DateTime.UtcNow,
                        Data = bytes
                    };

                    _context.UploadedFiles.Add(uploaded);
                    await _context.SaveChangesAsync();

                    // Reference uploaded file id via FilePath (legacy) for compatibility
                    thesis.FilePath = uploaded.Id;
                }
                catch
                {
                    // ignore invalid base64 -> keep FilePath if provided
                }
            }

            _context.Theses.Add(thesis);
            await _context.SaveChangesAsync();

            return MapToDto(thesis);
        }

        public async Task<ThesisDto> UpdateThesis(string id, UpdateThesisDto request)
        {
            if (!int.TryParse(id, out var intId))
                throw new Exception("Invalid thesis ID.");

            var thesis = await _context.Theses.FindAsync(intId)
                         ?? throw new Exception("Thesis not found.");

            if (!string.IsNullOrEmpty(request.Title))
                thesis.Title = request.Title;

            if (!string.IsNullOrEmpty(request.Abstract))
                thesis.Abstract = request.Abstract;

            if (request.Keywords != null && request.Keywords.Length > 0)
                thesis.Keywords = JsonSerializer.Serialize(request.Keywords);

            if (!string.IsNullOrEmpty(request.Authors))
                thesis.Authors = request.Authors;

            if (request.Advisors != null)
                thesis.Advisors = request.Advisors;

            if (!string.IsNullOrEmpty(request.Department))
                thesis.Department = request.Department;

            if (!string.IsNullOrEmpty(request.FieldOfResearch))
                thesis.FieldOfResearch = request.FieldOfResearch;

            if (request.Year.HasValue)
                thesis.Year = request.Year.Value;

            if (!string.IsNullOrEmpty(request.MainAuthorEmail))
                thesis.MainAuthorEmail = request.MainAuthorEmail;

            if (request.CoAuthorEmail != null)
                thesis.CoAuthorEmail = request.CoAuthorEmail;

            if (!string.IsNullOrEmpty(request.ResearchType))
                thesis.ResearchType = request.ResearchType;

            if (!string.IsNullOrEmpty(request.Doi))
                thesis.Doi = NormalizeDoi(request.Doi);

            if (!string.IsNullOrEmpty(request.Status))
            {
                var prevStatus = thesis.Status;
                thesis.Status = request.Status;

                // Set ApprovedAt and generate APA citation when transitioning to approved
                if (request.Status == "approved" && prevStatus != "approved")
                {
                    thesis.ApprovedAt = DateTime.UtcNow;
                    thesis.ApaCitation = GenerateApaCitation(thesis);
                    thesis.IeeeCitation = GenerateIeeeCitation(thesis);
                    thesis.AcsCitation  = GenerateAcsCitation(thesis);
                }
                
                // Track when it was rejected
                if (request.Status == "rejected" && prevStatus != "rejected")
                {
                    thesis.RejectedAt = DateTime.UtcNow;
                }
            }

            if (!string.IsNullOrEmpty(request.ApprovedBy) &&
                int.TryParse(request.ApprovedBy, out var approverId))
            {
                thesis.ApprovedBy = approverId;
            }

            if (request.RejectionReason != null)
                thesis.RejectionReason = request.RejectionReason;

            await _context.SaveChangesAsync();
            return MapToDto(thesis);
        }

        public async Task DeleteThesis(string id)
        {
            if (!int.TryParse(id, out var intId))
                throw new Exception("Invalid thesis ID.");

            var thesis = await _context.Theses.FindAsync(intId)
                         ?? throw new Exception("Thesis not found.");

            // Soft delete instead of hard delete
            thesis.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task IncrementViewCount(string id)
        {
            if (!int.TryParse(id, out var intId))
                throw new Exception("Invalid thesis ID.");

            var thesis = await _context.Theses.FindAsync(intId)
                         ?? throw new Exception("Thesis not found.");

            thesis.ViewCount += 1;
            await _context.SaveChangesAsync();
        }

        // ── PDF Handling ─────────────────────────────────────────────────────────

        public async Task<string> UploadPdf(string fileData)
        {
            // Store uploaded PDF directly in DB (UploadedFiles table) and return generated Id
            var base64Data = fileData;
            var commaIndex = fileData.IndexOf(',');
            if (commaIndex >= 0)
                base64Data = fileData[(commaIndex + 1)..];

            var bytes = Convert.FromBase64String(base64Data);

            var id = Guid.NewGuid().ToString();
            var uploaded = new Models.UploadedFile
            {
                Id = id,
                FileName = $"thesis_{DateTime.UtcNow.Ticks}.pdf",
                FileType = "application/pdf",
                UploadedAt = DateTime.UtcNow,
                Data = bytes
            };

            _context.UploadedFiles.Add(uploaded);
            await _context.SaveChangesAsync();

            return id;
        }

        public async Task<string?> GetPdfData(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                return null;

            // First try to load from UploadedFiles table (primary storage now)
            try
            {
                var uploaded = await _context.UploadedFiles.FindAsync(fileId);
                if (uploaded != null && uploaded.Data != null && uploaded.Data.Length > 0)
                {
                    return $"data:{uploaded.FileType ?? "application/pdf"};base64,{Convert.ToBase64String(uploaded.Data)}";
                }
            }
            catch
            {
                // ignore
            }

            // Backwards compatibility: if fileId is a filesystem path
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileId);
                if (File.Exists(filePath))
                {
                    var bytes = await File.ReadAllBytesAsync(filePath);
                    return $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}";
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        // ── Mapping ──────────────────────────────────────────────────────────────

        private static string GenerateApaCitation(Thesis thesis)
        {
            // APA Format: LastName, F., & LastName, F. (Year). Title.
            var authorsRaw = thesis.Authors ?? "Unknown Author";
            var year = thesis.Year > 0 ? thesis.Year.ToString() : DateTime.UtcNow.Year.ToString();
            var title = thesis.Title ?? "Untitled";

            var formattedAuthors = FormatApaAuthors(authorsRaw);

            return $"{formattedAuthors}. ({year}). {title}.";
        }

        private static string FormatApaAuthors(string authorsRaw)
        {
            var authors = authorsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();

            if (authors.Count == 0)
                return "Unknown Author";

            var formatted = authors.Select(FormatApaAuthorName).ToList();

            if (formatted.Count == 1)
                return formatted[0];

            if (formatted.Count == 2)
                return $"{formatted[0]}, & {formatted[1]}";

            return $"{string.Join(", ", formatted.Take(formatted.Count - 1))}, & {formatted.Last()}";
        }

        private static string FormatApaAuthorName(string author)
        {
            var parts = author.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "Unknown Author";

            var lastName = parts[^1];
            var initials = parts
                .Take(parts.Length - 1)
                .Select(p => char.ToUpperInvariant(p[0]) + ".")
                .ToArray();

            if (initials.Length == 0)
                return lastName;

            return $"{lastName}, {string.Join(" ", initials)}";
        }

        private static ThesisDto MapToDto(Thesis t)
        {
            string[] keywords;
            try
            {
                keywords = JsonSerializer.Deserialize<string[]>(t.Keywords)
                           ?? Array.Empty<string>();
            }
            catch
            {
                // Fall back: treat as comma-separated plain text
                keywords = t.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }

            var updatedAt = t.ApprovedAt ?? t.UploadedAt;

            return new ThesisDto
            {
                Id              = t.ThesisId.ToString(),
                Title           = t.Title,
                Abstract        = t.Abstract,
                Keywords        = keywords,
                Authors         = t.Authors,
                Advisors        = t.Advisors,
                Department      = t.Department,
                FieldOfResearch = t.FieldOfResearch,
                Year            = t.Year,
                PdfUrl          = t.FilePath,          // FilePath → PdfUrl for frontend (may contain UploadedFile Id)
                PdfData         = t.PdfData == null ? null : $"data:application/pdf;base64,{Convert.ToBase64String(t.PdfData)}",
                PdfFileId       = t.FilePath,
                Status          = t.Status,
                UploadedBy      = t.UploadedBy?.ToString(),
                ApprovedBy      = t.ApprovedBy?.ToString(),
                CreatedAt       = t.UploadedAt,        // UploadedAt → CreatedAt for frontend
                UpdatedAt       = updatedAt,
                ApprovedAt      = t.ApprovedAt,
                RejectionReason = t.RejectionReason,
                ViewCount       = t.ViewCount,
                DownloadCount   = t.DownloadCount,
                ApaCitation     = t.ApaCitation,
                Doi             = t.Doi,
                IeeeCitation    = t.IeeeCitation,
                AcsCitation     = t.AcsCitation,
                MainAuthorEmail = t.MainAuthorEmail,
                CoAuthorEmail   = t.CoAuthorEmail,
                ResearchType    = t.ResearchType
                ,IsPublished     = string.Equals(t.Status, "approved", StringComparison.Ordinal)
            };
        }

        private static string GenerateIeeeCitation(Thesis thesis)
        {
            // IEEE: F. Lastname and F. Lastname, "Title," Dept., Univ., Year. DOI
            var authors = thesis.Authors ?? "Unknown Author";
            var formatted = FormatIeeeAuthors(authors);
            var title = thesis.Title ?? "Untitled";
            var dept = thesis.Department ?? "Unknown Dept.";
            var year = thesis.Year > 0 ? thesis.Year.ToString() : DateTime.UtcNow.Year.ToString();

            var doiPart = string.IsNullOrWhiteSpace(thesis.Doi) ? string.Empty : $" DOI: {thesis.Doi}";

            return $"{formatted}, \"{title},\" {dept}, {year}.{doiPart}";
        }

        private static string FormatIeeeAuthors(string authorsRaw)
        {
            var authors = authorsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();

            if (authors.Count == 0)
                return "Unknown Author";

            // IEEE uses initials then last name: F. Lastname
            var formatted = authors.Select(a =>
            {
                var parts = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return a;
                var last = parts[^1];
                var initials = string.Join(".", parts.Take(parts.Length - 1).Select(p => p[0].ToString().ToUpper()));
                if (!string.IsNullOrEmpty(initials)) initials += ".";
                return $"{initials} {last}".Trim();
            });

            return string.Join(" and ", formatted);
        }

        private static string GenerateAcsCitation(Thesis thesis)
        {
            // ACS: Lastname, F.; Lastname, F. Title. Univ., Year. DOI
            var authorsRaw = thesis.Authors ?? "Unknown Author";
            var authors = authorsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();

            var formatted = authors.Select(a =>
            {
                var parts = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return a;
                var last = parts[^1];
                var initials = string.Join("", parts.Take(parts.Length - 1).Select(p => p[0].ToString().ToUpper() + "."));
                return string.IsNullOrEmpty(initials) ? last : $"{last}, {initials}";
            });

            var title = thesis.Title ?? "Untitled";
            var univ = thesis.Department ?? "Unknown Univ.";
            var year = thesis.Year > 0 ? thesis.Year.ToString() : DateTime.UtcNow.Year.ToString();
            var doiPart = string.IsNullOrWhiteSpace(thesis.Doi) ? string.Empty : $" DOI: {thesis.Doi}";

            return $"{string.Join("; ", formatted)}. {title}. {univ}, {year}.{doiPart}";
        }

        // Normalize DOI input: trim, remove common prefixes, return null for empty/invalid
        public static string? NormalizeDoi(string? doi)
        {
            if (string.IsNullOrWhiteSpace(doi))
                return null;

            var d = doi.Trim();

            // Remove common DOI URL prefixes (case-insensitive)
            if (d.StartsWith("https://doi.org/", StringComparison.OrdinalIgnoreCase))
                d = d.Substring("https://doi.org/".Length);
            else if (d.StartsWith("http://doi.org/", StringComparison.OrdinalIgnoreCase))
                d = d.Substring("http://doi.org/".Length);
            else if (d.StartsWith("doi:", StringComparison.OrdinalIgnoreCase))
                d = d.Substring("doi:".Length);

            d = d.Trim();

            // Basic length validation
            if (d.Length == 0 || d.Length > 255)
                return null;

            return d;
        }

        private static bool IsPdfAvailable(Thesis thesis)
        {
            // Consider PDF available if either a filesystem path exists or PDF binary is stored in DB.
            if (!string.IsNullOrWhiteSpace(thesis.FilePath))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), thesis.FilePath);
                if (File.Exists(filePath))
                    return true;
            }

            if (thesis.PdfData != null && thesis.PdfData.Length > 0)
                return true;

            return false;
        }
    }
}
