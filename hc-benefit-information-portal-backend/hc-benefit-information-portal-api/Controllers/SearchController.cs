using Microsoft.AspNetCore.Mvc;
using Meilisearch;

namespace hc_benefit_information_portal_api.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly MeilisearchClient _client;

        public SearchController()
        {
            _client = new MeilisearchClient("http://localhost:7700");
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            var index = _client.Index("benefits");

            var result = await index.SearchAsync<object>(q);

            return Ok(result.Hits);
        }
    }
}