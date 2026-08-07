using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using hc_benefit_information_portal_api.Data;
using hc_benefit_information_portal_api.Models;

namespace hc_benefit_information_portal_api.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        // Kebijakan lockout yang sudah disepakati: maks 5x gagal -> lock 15 menit
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<bool> IsLockedOutAsync(string nrp)
        {
            var since = DateTime.Now.AddMinutes(-LockoutMinutes);
            var failedCount = await _db.LoginAttemptsLog
                .Where(l => l.Nrp == nrp && l.Success == false && l.AttemptedAt >= since)
                .CountAsync();

            return failedCount >= MaxFailedAttempts;
        }

        public async Task LogAttemptAsync(string nrp, bool success, string? ipAddress)
        {
            _db.LoginAttemptsLog.Add(new LoginAttemptLog
            {
                Nrp = nrp,
                Success = success,
                IpAddress = ipAddress,
                AttemptedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        public async Task<Employee?> ValidateLoginAsync(string nrp, DateTime tanggalLahir)
        {
            return await _db.Employees.FirstOrDefaultAsync(e =>
                e.Nrp == nrp &&
                e.TanggalLahir.Date == tanggalLahir.Date &&
                e.IsActive);
        }

        public async Task<string?> GetRoleNameAsync(int? roleId)
        {
            if (roleId == null) return null;
            var role = await _db.Roles.FindAsync(roleId);
            return role?.Name;
        }

        public string GenerateJwtToken(Employee employee, string? roleName)
        {
            var jwtSection = _config.GetSection("Jwt");
            var secret = jwtSection["Secret"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiryHours = int.Parse(jwtSection["ExpiryHours"] ?? "8");

            var claims = new List<Claim>
            {
                new Claim("employee_id", employee.Id.ToString()),
                new Claim("nrp", employee.Nrp),
                new Claim("nama", employee.Nama),
                new Claim("email", employee.Email ?? ""),
                new Claim("role_id", employee.RoleId?.ToString() ?? ""),
                new Claim(ClaimTypes.Role, roleName ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}