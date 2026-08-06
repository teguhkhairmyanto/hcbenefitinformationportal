namespace hc_benefit_information_portal_api.Models
{
    public class LoginRequestDto
    {
        public string Nrp { get; set; } = string.Empty;

        // Dikirim frontend sebagai string 'YYYY-MM-DD', di-bind otomatis ke DateTime
        public DateTime TanggalLahir { get; set; }
    }
}