using System.ComponentModel.DataAnnotations.Schema;

namespace hc_benefit_information_portal_api.Models
{
    [Table("employees")]
    public class Employee
    {
        public int Id { get; set; }
        public string Nrp { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;

        [Column("tanggal_lahir")]
        public DateTime TanggalLahir { get; set; }

        [Column("golongan_id")]
        public int? GolonganId { get; set; }

        [Column("cabang_id")]
        public int? CabangId { get; set; }

        [Column("pangkat_id")]
        public int? PangkatId { get; set; }

        [Column("kelompok_jabatan_id")]
        public int? KelompokJabatanId { get; set; }

        [Column("status_keluarga_id")]
        public int? StatusKeluargaId { get; set; }

        [Column("role_id")]
        public int? RoleId { get; set; }

        [Column("role_source")]
        public string RoleSource { get; set; } = "rule";

        public string? Email { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("data_source")]
        public string DataSource { get; set; } = "excel_import";
    }
}