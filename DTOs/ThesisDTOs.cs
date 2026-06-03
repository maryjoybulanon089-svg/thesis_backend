using System.ComponentModel.DataAnnotations;

namespace ThesisRepository.DTOs
{
    /// <summary>
    /// Request body for creating a new thesis.
    /// UploadedBy is the string representation of the uploader's UserId.
    /// </summary>
    public class CreateThesisDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Abstract { get; set; } = string.Empty;

        [Required]
        public string[] Keywords { get; set; } = Array.Empty<string>();

        [Required]
        public string Authors { get; set; } = string.Empty;

        /// <summary>Advisor names (comma-separated).</summary>
        [Required]
        public string Advisors { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// Field / area of research.
        /// </summary>
        [Required]
        public string FieldOfResearch { get; set; } = string.Empty;

        [Required]
        public int Year { get; set; }

        /// <summary>
        /// PDF file path or URL. Maps to the SSMS column FilePath.
        /// </summary>
        public string? PdfUrl { get; set; }

        /// <summary>Base-64 encoded PDF sent on upload (not persisted to DB directly).</summary>
        public string? PdfData { get; set; }

        [Required]
        public string UploadedBy { get; set; } = string.Empty;  // string of UserId int

        /// <summary>Main author email address.</summary>
        public string? MainAuthorEmail { get; set; }

        /// <summary>Co-author email address (optional).</summary>
        public string? CoAuthorEmail { get; set; }

        /// <summary>Research type (White Paper or Published Research).</summary>
        public string? ResearchType { get; set; }

        /// <summary>Digital Object Identifier (DOI) for the thesis/article.</summary>
        public string? Doi { get; set; }
    }

    /// <summary>
    /// Request body for partially updating an existing thesis.
    /// </summary>
    public class UpdateThesisDto
    {
        public string? Title { get; set; }

        public string? Abstract { get; set; }

        public string[]? Keywords { get; set; }

        public string? Authors { get; set; }

        public string? Advisors { get; set; }

        public string? Department { get; set; }

        public string? FieldOfResearch { get; set; }

        public int? Year { get; set; }

        public string? Status { get; set; }

        public string? ApprovedBy { get; set; }  // string of UserId int
        public string? RejectionReason { get; set; }

        public string? MainAuthorEmail { get; set; }

        public string? CoAuthorEmail { get; set; }

        public string? ResearchType { get; set; }

        public string? Doi { get; set; }
    }

    /// <summary>
    /// Thesis data returned to the frontend.
    /// int ThesisId / UserId values are serialised as strings for frontend compatibility.
    /// </summary>
    public class ThesisDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;        // ThesisId.ToString()
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Abstract { get; set; } = string.Empty;
        
        [Required]
        public string[] Keywords { get; set; } = Array.Empty<string>();
        
        [Required]
        public string Authors { get; set; } = string.Empty;
        
        public string? Advisors { get; set; }
        
        [Required]
        public string Department { get; set; } = string.Empty;
        
        [Required]
        public string FieldOfResearch { get; set; } = string.Empty;
        
        [Required]
        public int Year { get; set; }

        /// <summary>FilePath value from DB, exposed as pdfUrl to frontend.</summary>
        public string? PdfUrl { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        /// <summary>UploadedBy UserId as string.</summary>
        public string? UploadedBy { get; set; }

        /// <summary>ApprovedBy UserId as string.</summary>
        public string? ApprovedBy { get; set; }

        /// <summary>Maps to UploadedAt column.</summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>Same as CreatedAt (SSMS schema has no UpdatedAt; ApprovedAt used if available).</summary>
        [Required]
        public DateTime UpdatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        [Required]
        public int ViewCount { get; set; }
        
        [Required]
        public int DownloadCount { get; set; }

        /// <summary>Auto-generated APA citation format.</summary>
        public string? ApaCitation { get; set; }

        /// <summary>Digital Object Identifier (DOI) for the thesis/article.</summary>
        public string? Doi { get; set; }

        /// <summary>Auto-generated IEEE citation format.</summary>
        public string? IeeeCitation { get; set; }

        /// <summary>Auto-generated ACS citation format.</summary>
        public string? AcsCitation { get; set; }

        /// <summary>Main author email address.</summary>
        public string? MainAuthorEmail { get; set; }

        /// <summary>Co-author email address.</summary>
        public string? CoAuthorEmail { get; set; }

        /// <summary>Research type (White Paper or Published Research).</summary>
        public string? ResearchType { get; set; }

        public string? Doi { get; set; }
    }

    public class UploadPdfDto
    {
        [Required]
        public string FileData { get; set; } = string.Empty;  // Base-64 encoded PDF
    }
}
