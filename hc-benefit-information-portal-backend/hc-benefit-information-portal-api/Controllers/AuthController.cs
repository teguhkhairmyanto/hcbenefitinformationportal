using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using hc_benefit_information_portal_api.Services;
using hc_benefit_information_portal_api.Models;
using System.Security.Claims;

namespace hc_benefit_information_portal_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nrp))
            {
                return BadRequest(new { message = "NRP wajib diisi." });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Cek lockout SEBELUM proses validasi (mencegah brute-force NRP+tanggal lahir)
            if (await _authService.IsLockedOutAsync(dto.Nrp))
            {
                return StatusCode(429, new { message = "Terlalu banyak percobaan gagal. Coba lagi dalam 15 menit." });
            }

            var employee = await _authService.ValidateLoginAsync(dto.Nrp, dto.TanggalLahir);

            if (employee == null)
            {
                await _authService.LogAttemptAsync(dto.Nrp, false, ip);
                return Unauthorized(new { message = "NRP atau tanggal lahir tidak sesuai." });
            }

            await _authService.LogAttemptAsync(dto.Nrp, true, ip);

            var roleName = await _authService.GetRoleNameAsync(employee.RoleId);
            var token = _authService.GenerateJwtToken(employee, roleName);

            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,           // WAJIB true di production (HTTPS). Di localhost tanpa HTTPS, lihat catatan di Program.cs
                SameSite = SameSiteMode.None, // None diperlukan krn frontend beda origin (5173) dari backend (5117)
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            return Ok(new LoginResponseDto
            {
                Nrp = employee.Nrp,
                Nama = employee.Nama,
                RoleName = roleName
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return Ok(new { message = "Logout berhasil." });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var nrp = User.FindFirst("nrp")?.Value;
            var nama = User.FindFirst("nama")?.Value;
            var roleId = User.FindFirst("role_id")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { nrp, nama, roleId, role });
        }
    }
}