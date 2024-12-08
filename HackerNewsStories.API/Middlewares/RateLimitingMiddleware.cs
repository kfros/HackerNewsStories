namespace HackerNewsStories.API.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SemaphoreSlim _semaphore;
        private const int MaxSimultaneousRequests = 20;

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
            _semaphore = new SemaphoreSlim(MaxSimultaneousRequests);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _semaphore.WaitAsync();
            try
            {
                await _next(context);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
