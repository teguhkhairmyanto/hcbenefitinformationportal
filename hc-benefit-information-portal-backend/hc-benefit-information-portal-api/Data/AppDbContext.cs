using Microsoft.EntityFrameworkCore;
using hc_benefit_information_portal_api.Models; // Pastikan namespace sesuai dengan folder Model Anda

namespace hc_benefit_information_portal_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Daftarkan semua tabel yang kita buat di Step 1
        public DbSet<Benefit> Benefits { get; set; }
        public DbSet<BenefitDetail> BenefitDetails { get; set; }
        public DbSet<BenefitTag> BenefitTags { get; set; }
        public DbSet<Tags> Tags { get; set; }
        public DbSet<Faq> Faqs { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<LoginAttemptLog> LoginAttemptsLog { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfigurasi khusus untuk tabel penghubung (Junction Table) benefit_tags
            // Karena tabel ini tidak punya ID tunggal (Composite Key)
            modelBuilder.Entity<BenefitTag>()
                .HasKey(bt => new { bt.BenefitId, bt.TagId });
        }
    }
}