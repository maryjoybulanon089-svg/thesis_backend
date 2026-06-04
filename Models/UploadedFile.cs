using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisRepository.Models
{
    [Table("UploadedFiles")]
    public class UploadedFile
    {
        [Key]
        [Column("Id")]
        [MaxLength(450)]
        public string Id { get; set; } = string.Empty; // GUID or random string

        [Column("FileName")]
        [MaxLength(255)]
        public string? FileName { get; set; }

        [Column("FileType")]
        [MaxLength(100)]
        public string? FileType { get; set; }

        [Column("UploadedAt")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Column("Data")]
        public byte[]? Data { get; set; }
    }
}
