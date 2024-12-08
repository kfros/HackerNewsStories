using HackerNewsStories.API.Models;

namespace HackerNewsStories.API.Services.Interfaces
{
    public interface IHackerNewsService
    {
        Task<IEnumerable<Story>> GetBestStoriesAsync(int count);
    }
}
