namespace HackerNewsStories.API.Models
{
    public class StoryDetail
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? By { get; set; }
        public long Time { get; set; }
        public int Score { get; set; }
        public string? Type { get; set; }
        public int Descendants { get; set; }
    }
}
