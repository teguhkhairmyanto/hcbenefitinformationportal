using System.ComponentModel.DataAnnotations.Schema;

namespace hc_benefit_information_portal_api.Models
{
    [Table("faq")]
    public class Faq
    {
        public int Id { get; set; }
        
        [Column("benefit_id")]
        public int BenefitId { get; set; }
        
        public string Question { get; set; }
        public string? Answer { get; set; }
        
        [Column("sort_order")]
        public int? SortOrder { get; set; }
    }
}