using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using hc_benefit_information_portal_api.Services;
using hc_benefit_information_portal_api.Models; // Tambahkan ini untuk mengenali DTO
using System;
using System.Threading.Tasks;

namespace hc_benefit_information_portal_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BenefitsController : ControllerBase
    {
        private readonly BenefitService _service;

        public BenefitsController(BenefitService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? categoryId)
        {
            var data = await _service.GetAllBenefits(categoryId);
            return Ok(data);
        }

        // ==========================================
        // 🔹 BARU: Benefit sesuai entitlement karyawan yang login
        // ==========================================
        [HttpGet("my-benefits")]
        [Authorize]
        public async Task<IActionResult> GetMyBenefits([FromQuery] int? categoryId)
        {
            var roleIdClaim = User.FindFirst("role_id")?.Value;

            if (string.IsNullOrEmpty(roleIdClaim) || !int.TryParse(roleIdClaim, out int roleId))
            {
                // Karyawan belum punya role_id (masih NULL) -> tidak ada benefit yang entitled
                return Ok(new System.Collections.Generic.List<object>());
            }

            var data = await _service.GetBenefitsForRole(categoryId, roleId);
            return Ok(data);
        }
        // ==========================================
        // TAMBAHKAN INI: Endpoint untuk Dashboard
        // ==========================================
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            // Pastikan method GetBenefitCountByCategory() sudah ada di BenefitService.cs
            var data = await _service.GetBenefitCountByCategory();
            return Ok(data);
        }
        // ==========================================
        // 🔹 TAMBAHKAN INI: Endpoint untuk Simpan Data (Step 6)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CreateBenefit([FromBody] BenefitCreateDto dto)
        {
            // Validasi dasar
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(new { message = "Data tidak valid. Judul wajib diisi." });
            }

            try
            {
                // Memanggil method yang kita buat di Step 5
                var success = await _service.CreateBenefitAsync(dto);

                if (success)
                {
                    return Ok(new { message = "Benefit berhasil disimpan!" });
                }

                return StatusCode(500, new { message = "Gagal menyimpan data ke database." });
            }
            catch (Exception ex)
            {
                // Mencatat error ke console backend untuk pengecekan
                Console.WriteLine($"Error pada CreateBenefit: {ex.Message}");
                return StatusCode(500, new { message = "Terjadi kesalahan internal: " + ex.Message });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBenefit(int id, [FromBody] BenefitCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Data tidak valid." });

            try
            {
                var success = await _service.UpdateBenefitAsync(id, dto);
                if (success) return Ok(new { message = "Benefit berhasil diperbarui!" });
                return NotFound(new { message = "Benefit tidak ditemukan." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Update: {ex.Message}");
                return StatusCode(500, new { message = "Gagal memperbarui data." });
            }
        }

        // 🔹 TAMBAHKAN INI: Endpoint untuk Delete (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBenefit(int id)
        {
            try
            {
                var success = await _service.DeleteBenefitAsync(id);
                if (success) return Ok(new { message = "Benefit berhasil dihapus!" });
                return NotFound(new { message = "Benefit tidak ditemukan." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Delete: {ex.Message}");
                return StatusCode(500, new { message = "Gagal menghapus data." });
            }
        }
    }
}