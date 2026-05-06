using System.ComponentModel.DataAnnotations.Schema;

namespace hc_benefit_information_portal_api.Models
{
    [Table("tags")]
    public class Tags
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}