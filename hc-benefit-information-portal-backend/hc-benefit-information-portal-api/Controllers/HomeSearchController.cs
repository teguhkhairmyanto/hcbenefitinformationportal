using Microsoft.AspNetCore.Mvc;
using Meilisearch;
using System.Text.Json;

namespace hc_benefit_information_portal_api.Controllers
{
    [ApiController]
    [Route("api/home-search")]
    public class HomeSearchController : ControllerBase
    {
        private readonly MeilisearchClient _meiliClient;

        public HomeSearchController()
        {
            // Pastikan URL Meilisearch sesuai dengan instance kamu
            _meiliClient = new MeilisearchClient("http://localhost:7700");
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Ok(new
                {
                    bestAnswer = (object)null,
                    suggestions = new List<object>()
                });
            }

            // Sanitasi input
            var query = keyword.Trim().ToLower();

            var benefitIndex = _meiliClient.Index("benefits");
            var faqIndex = _meiliClient.Index("faqs");

            // ==========================================================
            // 🔹 CONFIGURATION: MatchingStrategy.All
            // Memastikan jika user ketik 2 kata, kedua kata itu WAJIB ada.
            // Ini mencegah "Bantuan Duka" tenggelam oleh hasil "Bantuan" saja.
            // ==========================================================
            var searchParams = new SearchQuery
            {
                Limit = 15,
                AttributesToHighlight = new[] { "title", "description", "question", "answer" },
                MatchingStrategy = "all" 
            };

            try
            {
                // Eksekusi pencarian secara paralel untuk performa maksimal
                var benefitTask = benefitIndex.SearchAsync<JsonElement>(query, searchParams);
                var faqTask = faqIndex.SearchAsync<JsonElement>(query, searchParams);

                await Task.WhenAll(benefitTask, faqTask);

                // 1. Mapping Benefit Results
                var benefitSuggestions = benefitTask.Result.Hits.Select(b => new
                {
                    type = "benefit",
                    id = b.GetProperty("id").GetInt32(),
                    title = b.GetProperty("title").GetString(),
                    description = b.GetProperty("description").GetString(),
                    // Ambil hasil highlight dari Meilisearch jika ada
                    highlight = b.TryGetProperty("_formatted", out var formatted)
                                ? formatted.GetProperty("title").GetString()
                                : b.GetProperty("title").GetString()
                }).ToList();

                // 2. Mapping FAQ Results
                var faqSuggestions = faqTask.Result.Hits.Select(f => new
                {
                    type = "faq",
                    id = f.GetProperty("id").GetInt32(),
                    title = f.GetProperty("question").GetString(), // Question sebagai title di UI
                    subtitle = f.TryGetProperty("benefitTitle", out var bt) ? bt.GetString() : "Umum",
                    answer = f.GetProperty("answer").GetString(),
                    highlight = f.TryGetProperty("_formatted", out var formatted)
                                ? formatted.GetProperty("question").GetString()
                                : f.GetProperty("question").GetString()
                }).ToList();

                // ==========================================================
                // 🔹 LOGIKA PENGGABUNGAN (COMBINING)
                // Kita gabungkan keduanya, urutan akan di-sort ulang di Frontend
                // atau kita taruh Benefit di atas jika judulnya mengandung keyword.
                // ==========================================================
                var combined = new List<object>();
                
                // Prioritaskan Benefit yang mengandung kata kunci di judulnya
                var priorityBenefits = benefitSuggestions
                    .Where(b => b.title.ToLower().Contains(query)).ToList();
                var otherBenefits = benefitSuggestions.Except(priorityBenefits).ToList();

                combined.AddRange(priorityBenefits);
                combined.AddRange(faqSuggestions);
                combined.AddRange(otherBenefits);

                // ==========================================================
                // 🔹 DETEKSI BEST ANSWER
                // FAQ tetap menjadi prioritas utama untuk jawaban langsung.
                // ==========================================================
                object bestAnswer = null;
                if (faqSuggestions.Any())
                {
                    bestAnswer = new
                    {
                        type = "faq",
                        answer = faqSuggestions.First().answer
                    };
                }
                else if (priorityBenefits.Any())
                {
                    bestAnswer = new
                    {
                        type = "benefit",
                        answer = priorityBenefits.First().description
                    };
                }

                return Ok(new
                {
                    bestAnswer,
                    suggestions = combined
                });
            }
            catch (Exception ex)
            {
                // Log error jika diperlukan
                return StatusCode(500, new { message = "Gagal melakukan pencarian", detail = ex.Message });
            }
        }
    }
}