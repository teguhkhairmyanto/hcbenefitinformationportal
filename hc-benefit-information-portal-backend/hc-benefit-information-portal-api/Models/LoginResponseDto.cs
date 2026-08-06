namespace hc_benefit_information_portal_api.Models
{
    public class LoginResponseDto
    {
        public string Nrp { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string? RoleName { get; set; }
    }
}