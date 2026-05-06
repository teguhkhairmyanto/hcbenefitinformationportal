using System.ComponentModel.DataAnnotations.Schema;

namespace hc_benefit_information_portal_api.Models
{
    [Table("benefit_tags")]
    public class BenefitTag
    {
        [Column("benefit_id")]
        public int BenefitId { get; set; }
        
        [Column("tag_id")]
        public int TagId { get; set; }
    }
}