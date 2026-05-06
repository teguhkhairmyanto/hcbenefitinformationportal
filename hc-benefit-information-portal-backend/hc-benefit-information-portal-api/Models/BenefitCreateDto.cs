using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace hc_benefit_information_portal_api.Models
{
    public class BenefitCreateDto
    {
        public string Title { get; set; }
        public int Category { get; set; }
        public string Description { get; set; }
        
        // Menangkap data dari formData.details (Key 1-6 dari Vue)
        public Dictionary<int, string> Details { get; set; }
        
        // Menangkap data dari formData.tags (Array string)
        public List<string>? Tags { get; set; }
        
        // Menangkap data dari formData.faqs (Array objek)
        public List<FaqDto>? Faqs { get; set; }
    }

    public class FaqDto
    {
        [JsonPropertyName("question")]
        public string Question { get; set; }

        [JsonPropertyName("answer")]
        public string Answer { get; set; }
    }
}