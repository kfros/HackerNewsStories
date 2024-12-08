using HackerNewsStories.API.Models;
using HackerNewsStories.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsStories.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;

        public StoriesController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Story>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBestStories([FromQuery] int count = 10)
        {
            if (count <= 0)
            {
                return BadRequest("Count must be a positive number.");
            }

            var stories = await _hackerNewsService.GetBestStoriesAsync(count);
            return Ok(stories);
        }
    }
}
