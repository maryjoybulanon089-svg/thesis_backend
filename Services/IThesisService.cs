using ThesisRepository.DTOs;

namespace ThesisRepository.Services
{
    public interface IThesisService
    {
        Task<List<ThesisDto>> GetAllTheses();
        Task<ThesisDto?> GetThesisById(string id);
        Task<ThesisDto> CreateThesis(CreateThesisDto request);
        Task<ThesisDto> UpdateThesis(string id, UpdateThesisDto request);
        Task DeleteThesis(string id);
        Task<string> UploadPdf(string fileData);
        Task<string?> GetPdfData(string fileId);
        Task<List<ThesisDto>> SearchTheses(string? query, string? department, string? fieldOfResearch, int? year, string? status = "approved", string? researchType = null);
        Task IncrementViewCount(string id);
    }
}
