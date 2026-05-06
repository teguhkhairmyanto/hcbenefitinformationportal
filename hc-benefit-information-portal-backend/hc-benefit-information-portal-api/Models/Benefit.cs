using System.ComponentModel.DataAnnotations.Schema;

namespace hc_benefit_information_portal_api.Models
{
    [Table("benefits")]
    public class Benefit
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; } // tipe text di DB
        
        [Column("category_id")]
        public int? CategoryId { get; set; }
        
        [Column("is_active")]
        public bool IsActive { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}