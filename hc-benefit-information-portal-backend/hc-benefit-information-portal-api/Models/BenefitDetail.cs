using System.ComponentModel.DataAnnotations.Schema;

namespace hc_benefit_information_portal_api.Models
{
    [Table("benefit_details")]
    public class BenefitDetail
    {
        public int Id { get; set; }
        
        [Column("benefit_id")]
        public int BenefitId { get; set; }
        
        [Column("section_title_id")]
        public int SectionTitleId { get; set; }
        
        public string? Content { get; set; }
        
        [Column("sort_order")]
        public int? SortOrder { get; set; }
    }
}