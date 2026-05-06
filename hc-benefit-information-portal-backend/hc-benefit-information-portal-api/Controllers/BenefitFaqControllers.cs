using Microsoft.AspNetCore.Mvc;
using hc_benefit_information_portal_api.Services;
using hc_benefit_information_portal_api.Models;

namespace hc_benefit_information_portal_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BenefitFaqController : ControllerBase
    {
        private readonly BenefitFaqServices _service;

        public BenefitFaqController(BenefitFaqServices service)
        {
            _service = service;
        }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? benefitId)
    {
        // Jika benefitId ada isinya, panggil GetFaqByBenefitId
        // Jika benefitId null, panggil GetAllFaq
        var data = benefitId.HasValue 
            ? await _service.GetFaqByBenefitId(benefitId.Value) 
            : await _service.GetAllFaq();

        return Ok(data);
    }

    [HttpPut("benefit/{benefitId}")]
    public async Task<IActionResult> UpdateFaq(int benefitId, [FromBody] List<FaqDto> faqs)
    {
        if (benefitId <= 0) return BadRequest("Invalid Benefit ID");

        var result = await _service.UpdateFaqByBenefitId(benefitId, faqs);

        if (result)
        {
            return Ok(new { message = "FAQ berhasil diperbarui dan sinkron ke Meilisearch" });
        }

        return StatusCode(500, "Gagal memperbarui FAQ");
    }
    


        
    }
}