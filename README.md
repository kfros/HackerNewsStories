# HackerNewsStories

## Overview
This ASP.NET Core Web API retrieves and serves the top N best stories from Hacker News, with efficient caching and concurrent fetching.

## Running the Application
1. Ensure .NET 6+ SDK is installed
2. Clone the repository
3. Navigate to project directory
4. Run `dotnet restore`
5. Run `dotnet run`
6. Access Swagger UI at `/swagger`

## Endpoint Usage
`GET /api/stories?count=10` 
- Retrieves top 10 stories by default
- `count` parameter allows specifying number of stories

## Assumptions
- Hacker News API is publicly accessible
- Temporary in-memory caching is acceptable
- Stories are primarily text/link posts

## Potential Enhancements
- Add distributed caching (Redis)
- Move Hacker News urls into config
- Implement circuit breaker for external API
- Add logging
- Create rate limiting middleware
- Add authentication/authorization