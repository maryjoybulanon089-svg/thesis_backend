using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisRepository.Models
{
    // Maps to the SSMS table: ThesisRepositoryDB.dbo.Users
    [Table("Users")]
    public class User
    {
        // INT IDENTITY(1,1) PRIMARY KEY
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("UserId")]
        public int UserId { get; set; }

        // NVARCHAR(100) NOT NULL
        [Required]
        [MaxLength(100)]
        [Column("FirstName")]
        public string FirstName { get; set; } = string.Empty;

        // NVARCHAR(100) NOT NULL
        [Required]
        [MaxLength(100)]
        [Column("LastName")]
        public string LastName { get; set; } = string.Empty;

        // NVARCHAR(255) NOT NULL UNIQUE
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        // NVARCHAR(MAX) NOT NULL
        [Required]
        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        // NVARCHAR(50) NOT NULL CHECK (Role IN ('admin','uploader','approver','faculty','student','guest'))
        [Required]
        [MaxLength(50)]
        [Column("Role")]
        public string Role { get; set; } = string.Empty;

        // NVARCHAR(100) - nullable
        [MaxLength(100)]
        [Column("Department")]
        public string? Department { get; set; }

        // boolean NOT NULL DEFAULT 1
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        // boolean NOT NULL DEFAULT 0  — added via 2_AlterDatabase.sql if not present
        [Column("IsApproved")]
        public bool IsApproved { get; set; } = false;

        // DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // DATETIME2 - nullable
        [Column("LastLogin")]
        public DateTime? LastLogin { get; set; }
    }
}
