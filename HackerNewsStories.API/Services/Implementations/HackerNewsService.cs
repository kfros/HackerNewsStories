using HackerNewsStories.API.Models;
using HackerNewsStories.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HackerNewsStories.API.Services.Implementations
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private const string BestStoriesUrl = "https://hacker-news.firebaseio.com/v0/beststories.json";
        private const string ItemUrlTemplate = "https://hacker-news.firebaseio.com/v0/item/{0}.json";
        private const string CacheKey = "BestStories";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

        public HackerNewsService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }
        
        public async Task<IEnumerable<Story>> GetBestStoriesAsync(int count)
        {
            if (_memoryCache.TryGetValue(CacheKey, out IEnumerable<Story>? cachedStories))
            {
                if (cachedStories != null) return cachedStories.Take(count);
            }

            var bestStoryIds = await FetchBestStoryIdsAsync();

            var stories = await FetchStoryDetailsAsync(bestStoryIds);

            // Sort and cache stories
            var sortedStories = stories
                .OrderByDescending(s => s.Score)
                .ToList();

            _memoryCache.Set(CacheKey, sortedStories, _cacheDuration);

            return sortedStories.Take(count);
        }

        private async Task<int[]> FetchBestStoryIdsAsync()
        {
            var response = await _httpClient.GetStringAsync(BestStoriesUrl);
            return JsonSerializer.Deserialize<int[]>(response)
                ?? Array.Empty<int>();
        }

        private async Task<List<Story>> FetchStoryDetailsAsync(int[] storyIds)
        {
            var tasks = storyIds.Select(async id =>
            {
                try
                {
                    var itemUrl = string.Format(ItemUrlTemplate, id);
                    var json = await _httpClient.GetStringAsync(itemUrl);
                    var story = JsonSerializer.Deserialize<StoryDetail>(json);

                    return story != null && story.Type == "story" ?
                        new Story
                        {
                            Title = story.Title ?? "",
                            Uri = story.Url ?? "",
                            PostedBy = story.By ?? "",
                            Time = DateTimeOffset.FromUnixTimeSeconds(story.Time),
                            Score = story.Score,
                            CommentCount = story.Descendants
                        } : null;
                }
                catch
                {
                    return null;
                }
            });

            var stories = await Task.WhenAll(tasks);
            return stories.Where(s => s != null).ToList()!;
        }
    }
}
