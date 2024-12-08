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
        private readonly SemaphoreSlim _semaphore;
        private const string BestStoriesUrl = "https://hacker-news.firebaseio.com/v0/beststories.json";
        private const string ItemUrlTemplate = "https://hacker-news.firebaseio.com/v0/item/{0}.json";
        private const string CacheKey = "BestStories";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
        private const int MaxConcurrentRequests = 5;
        private const int RequestDelayMilliseconds = 100;

        public HackerNewsService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _semaphore = new SemaphoreSlim(MaxConcurrentRequests);
        }

        public async Task<IEnumerable<Story>> GetBestStoriesAsync(int count)
        {
            if (_memoryCache.TryGetValue(CacheKey, out IEnumerable<Story>? cachedStories))
            {
                return cachedStories.Take(count);
            }

            var bestStoryIds = await FetchBestStoryIdsAsync();
            var stories = await FetchStoryDetailsWithRateLimitingAsync(bestStoryIds);

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

        private async Task<List<Story>> FetchStoryDetailsWithRateLimitingAsync(int[] storyIds)
        {
            var stories = new List<Story>();
            var tasks = new List<Task>();

            foreach (var id in storyIds)
            {
                await _semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(RequestDelayMilliseconds);
                            var story = await FetchSingleStoryAsync(id);
                            if (story != null)
                            {
                                lock (stories)
                                {
                                    stories.Add(story);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Log or handle specific exceptions if needed
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }
                ));
            }

            await Task.WhenAll(tasks);
            return stories;
        }

        private async Task<Story?> FetchSingleStoryAsync(int id)
        {
            try
            {
                var itemUrl = string.Format(ItemUrlTemplate, id);
                var response = await _httpClient.GetAsync(itemUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(1000);
                    response = await _httpClient.GetAsync(itemUrl);
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var story = JsonSerializer.Deserialize<StoryDetail>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return story?.Type == "story" ? new Story
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
        }
    }
}
