using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using ThesisRepository.DTOs;
using ThesisRepository.Services;

namespace ThesisRepository.Controllers
{
    [ApiController]
    [Route("api/thesis")]
    public class ThesesController : ControllerBase
    {
        private readonly IThesisService _thesisService;
        private readonly ILogger<ThesesController> _logger;
        private static readonly HashSet<string> AllowedResearchTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "White Paper",
                "Published Research",
                "Unpublished Paper"
            };

        private const string ResearchTypeValidationMessage = "Invalid research type. Must be 'White Paper', 'Published Research', or 'Unpublished Paper'.";

        public ThesesController(IThesisService thesisService, ILogger<ThesesController> logger)
        {
            _thesisService = thesisService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<ThesisDto>>> GetAllTheses()
        {
            try
            {
                var theses = await _thesisService.GetAllTheses();
                return Ok(theses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<ThesisDto>>> SearchTheses([FromQuery] string? query, [FromQuery] string? department, [FromQuery] string? fieldOfResearch, [FromQuery] int? year, [FromQuery] string? status = "approved", [FromQuery] string? researchType = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(researchType))
                {
                    _logger.LogInformation("SearchTheses received researchType='{researchTypeRaw}'", researchType);
                    var rt = researchType.Trim();
                    var rtLower = rt.ToLowerInvariant();
                    var allowedRt = AllowedResearchTypes.Contains(rt) || rtLower.Contains("unpubl");
                    _logger.LogInformation("SearchTheses normalized researchType='{researchType}' allowed={allowed}", rt, allowedRt);
                    if (!allowedRt)
                        return BadRequest(new { message = ResearchTypeValidationMessage });
                }

                var theses = await _thesisService.SearchTheses(query, department, fieldOfResearch, year, status, researchType);
                return Ok(theses);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ThesisDto>> GetThesisById(string id)
        {
            try
            {
                var thesis = await _thesisService.GetThesisById(id);
                if (thesis == null)
                    return NotFound(new { message = "Thesis not found" });

                return Ok(thesis);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ThesisDto>> CreateThesis([FromBody] CreateThesisDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ResearchType))
                    return BadRequest(new { message = "Research type is required." });

                _logger.LogInformation("CreateThesis received researchTypeRaw='{researchTypeRaw}'", request.ResearchType);
                var reqRt = request.ResearchType.Trim();
                var reqRtLower = reqRt.ToLowerInvariant();
                var allowedReq = AllowedResearchTypes.Contains(reqRt) || reqRtLower.Contains("unpubl");
                _logger.LogInformation("CreateThesis normalized researchType='{researchType}' allowed={allowed}", reqRt, allowedReq);
                if (!allowedReq)
                    return BadRequest(new { message = ResearchTypeValidationMessage });

                // Validate DOI if provided
                if (!string.IsNullOrWhiteSpace(request.Doi))
                {
                    var normalized = ThesisRepository.Services.ThesisService.NormalizeDoi(request.Doi);
                    if (normalized == null)
                        return BadRequest(new { message = "Invalid DOI." });

                    // Basic DOI pattern: starts with 10.<publisher id>/<suffix>
                    var doiPattern = new System.Text.RegularExpressions.Regex("^10\\.\\d{4,9}/\\S+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (!doiPattern.IsMatch(normalized))
                        return BadRequest(new { message = "Invalid DOI format." });
                }

                var thesis = await _thesisService.CreateThesis(request);
                return Ok(thesis);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<ThesisDto>> UpdateThesis(
            string id, 
            [FromBody] UpdateThesisDto request)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(request.ResearchType))
                {
                    _logger.LogInformation("UpdateThesis received researchTypeRaw='{researchTypeRaw}'", request.ResearchType);
                    var reqRt2 = request.ResearchType.Trim();
                    var reqRt2Lower = reqRt2.ToLowerInvariant();
                    var allowedReq2 = AllowedResearchTypes.Contains(reqRt2) || reqRt2Lower.Contains("unpubl");
                    _logger.LogInformation("UpdateThesis normalized researchType='{researchType}' allowed={allowed}", reqRt2, allowedReq2);
                    if (!allowedReq2)
                        return BadRequest(new { message = ResearchTypeValidationMessage });
                }

                // Validate DOI if provided
                if (!string.IsNullOrWhiteSpace(request.Doi))
                {
                    var normalized = ThesisRepository.Services.ThesisService.NormalizeDoi(request.Doi);
                    if (normalized == null)
                        return BadRequest(new { message = "Invalid DOI." });

                    var doiPattern = new System.Text.RegularExpressions.Regex("^10\\.\\d{4,9}/\\S+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (!doiPattern.IsMatch(normalized))
                        return BadRequest(new { message = "Invalid DOI format." });
                }

                var thesis = await _thesisService.UpdateThesis(id, request);
                return Ok(thesis);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteThesis(string id)
        {
            try
            {
                await _thesisService.DeleteThesis(id);
                return Ok(new { message = "Thesis deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/view")]
        public async Task<ActionResult> IncrementViewCount(string id)
        {
            try
            {
                await _thesisService.IncrementViewCount(id);
                return Ok(new { message = "View count incremented successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload-pdf")]
        public async Task<ActionResult<string>> UploadPdf([FromBody] UploadPdfDto request)
        {
            try
            {
                var fileId = await _thesisService.UploadPdf(request.FileData);
                // return the db id
                return Ok(new { fileId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Changed to file/{**fileId} based on uniform GET file id rules
        [HttpGet("file/{**fileId}")]
        public async Task<ActionResult<string>> GetPdfData(string fileId)
        {
            try
            {
                var data = await _thesisService.GetPdfData(fileId);
                if (data == null)
                    return NotFound(new { message = "PDF not found on server." });

                return Ok(new { data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Added this extra endpoint in case your frontend tries to fetch the PDF using the actual Thesis ID
        [HttpGet("{id}/pdf")]
        public async Task<ActionResult<string>> GetPdfByThesisId(string id)
        {
            try
            {
                var thesis = await _thesisService.GetThesisById(id);
                if (thesis == null)
                    return NotFound(new { message = "Thesis not found" });

                // Prefer filesystem-stored PDF when available
                if (!string.IsNullOrEmpty(thesis.PdfUrl))
                {
                    var data = await _thesisService.GetPdfData(thesis.PdfUrl);
                    if (data == null)
                        return NotFound(new { message = "PDF file not found on server." });

                    return Ok(new { data });
                }

                // Fall back to DB-stored PDF data (data URL) if present in the thesis DTO
                if (!string.IsNullOrEmpty(thesis.PdfData))
                {
                    return Ok(new { data = thesis.PdfData });
                }

                return NotFound(new { message = "Thesis or PDF not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Alias endpoint requested by frontend for viewing/downloading PDF by thesis ID.
        [HttpGet("{id}/pd")]
        public async Task<ActionResult> GetPdfDocumentByThesisId(string id)
        {
            try
            {
                var thesis = await _thesisService.GetThesisById(id);
                var thesis = await _thesisService.GetThesisById(id);
                if (thesis == null || string.IsNullOrEmpty(thesis.PdfUrl))
                    return NotFound(new { message = "Thesis or PDF path not found." });

                var data = await _thesisService.GetPdfData(thesis.PdfUrl);
                if (data == null)
                    return NotFound(new { message = "PDF file not found on server." });

                var base64 = data;
                var commaIndex = data.IndexOf(',');
                if (commaIndex >= 0 && commaIndex < data.Length - 1)
                    base64 = data[(commaIndex + 1)..];

                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(base64);
                }
                catch
                {
                    return BadRequest(new { message = "Stored PDF data is invalid." });
                }

                return File(bytes, "application/pdf", $"{id}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
