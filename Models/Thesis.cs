using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisRepository.Models
{
    // Maps to the SSMS table: ThesisRepositoryDB.dbo.Theses
    [Table("Theses")]
    public class Thesis
    {
        // INT IDENTITY(1,1) PRIMARY KEY
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ThesisId")]
        public int ThesisId { get; set; }

        // NVARCHAR(500) NOT NULL
        [Required]
        [MaxLength(500)]
        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        // NVARCHAR(MAX) NOT NULL
        [Required]
        [Column("Abstract")]
        public string Abstract { get; set; } = string.Empty;

        // NVARCHAR(200) NOT NULL
        [Required]
        [MaxLength(200)]
        [Column("Authors")]
        public string Authors { get; set; } = string.Empty;

        // NVARCHAR(200) - nullable
        [MaxLength(200)]
        [Column("Advisors")]
        public string? Advisors { get; set; }

        // NVARCHAR(100) NOT NULL
        [Required]
        [MaxLength(100)]
        [Column("Department")]
        public string Department { get; set; } = string.Empty;

        // NVARCHAR(150) NOT NULL
        [Required]
        [MaxLength(150)]
        [Column("FieldOfResearch")]
        public string FieldOfResearch { get; set; } = string.Empty;

        // INT NOT NULL
        [Required]
        [Column("Year")]
        public int Year { get; set; }

        // NVARCHAR(500) NOT NULL  — stored as JSON array string e.g. ["ml","iot"]
        [MaxLength(500)]
        [Column("Keywords")]
        public string Keywords { get; set; } = string.Empty;

        // NVARCHAR(1000) - nullable (replaces PdfUrl/PdfData)
        [MaxLength(1000)]
        [Column("FilePath")]
        public string? FilePath { get; set; }

        // NVARCHAR(50) NOT NULL DEFAULT 'pending'
        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "pending";

        // INT - nullable FK → Users(UserId) ON DELETE SET NULL
        [Column("UploadedBy")]
        public int? UploadedBy { get; set; }

        // INT - nullable (no FK constraint in original schema, but references UserId logically)
        [Column("ApprovedBy")]
        public int? ApprovedBy { get; set; }

        // DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP
        [Column("UploadedAt")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // DATETIME2 - nullable
        [Column("ApprovedAt")]
        public DateTime? ApprovedAt { get; set; }

        // DATETIME2 - nullable
        [Column("RejectedAt")]
        public DateTime? RejectedAt { get; set; }

        // boolean NOT NULL DEFAULT 0
        [Column("IsDeleted")]
        public bool IsDeleted { get; set; } = false;

        // NVARCHAR(MAX) - nullable
        [Column("RejectionReason")]
        public string? RejectionReason { get; set; }

        // INT NOT NULL DEFAULT 0
        [Column("ViewCount")]
        public int ViewCount { get; set; } = 0;

        // INT NOT NULL DEFAULT 0
        [Column("DownloadCount")]
        public int DownloadCount { get; set; } = 0;

        // NVARCHAR(255) - nullable - main author email address
        [MaxLength(255)]
        [Column("MainAuthorEmail")]
        public string? MainAuthorEmail { get; set; }

        // NVARCHAR(255) - nullable - co-author email address
        [MaxLength(255)]
        [Column("CoAuthorEmail")]
        public string? CoAuthorEmail { get; set; }

        // NVARCHAR(MAX) - nullable - auto-generated APA citation
        [Column("ApaCitation")]
        public string? ApaCitation { get; set; }

        // NVARCHAR(50) - nullable - research type (White Paper or Published Research)
        [MaxLength(50)]
        [Column("ResearchType")]
        public string? ResearchType { get; set; }
    }
}
