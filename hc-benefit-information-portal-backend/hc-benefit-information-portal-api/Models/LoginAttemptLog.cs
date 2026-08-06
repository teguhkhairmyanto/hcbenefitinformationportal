using System.ComponentModel.DataAnnotations.Schema;

namespace hc_benefit_information_portal_api.Models
{
    [Table("login_attempts_log")]
    public class LoginAttemptLog
    {
        public int Id { get; set; }
        public string Nrp { get; set; } = string.Empty;
        public bool Success { get; set; }

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("attempted_at")]
        public DateTime AttemptedAt { get; set; } = DateTime.Now;
    }
}