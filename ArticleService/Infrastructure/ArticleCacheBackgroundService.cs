using Shared.Models;
using CacheService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ArticleService.Infrastructure;
using ArticleService.Models;

namespace ArticleService.Infrastructure
{
    public class ArticleCacheBackgroundService : BackgroundService
    {
        private readonly ILogger<ArticleCacheBackgroundService> _logger;
        private readonly ArticleCacheService _cacheService;
        private readonly Dictionary<string, DbContext> _dbContexts;

        public ArticleCacheBackgroundService(
            ILogger<ArticleCacheBackgroundService> logger,
            ArticleCacheService cacheService,
            AfricaDbContext africa,
            AsiaDbContext asia,
            EuropeDbContext europe,
            NorthAmericaDbContext northAmerica,
            SouthAmericaDbContext southAmerica,
            OceaniaDbContext oceania,
            AntarcticaDbContext antarctica)
        {
            _logger = logger;
            _cacheService = cacheService;

            // Hold references til alle kontinenter
            _dbContexts = new(StringComparer.OrdinalIgnoreCase)
            {
                ["africa"] = africa,
                ["asia"] = asia,
                ["europe"] = europe,
                ["northamerica"] = northAmerica,
                ["southamerica"] = southAmerica,
                ["oceania"] = oceania,
                ["antarctica"] = antarctica
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Article cache background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Refreshing article cache for all continents...");

                try
                {
                    foreach (var kvp in _dbContexts)
                    {
                        var continent = kvp.Key;
                        var db = kvp.Value;

                        var sinceDate = DateTime.UtcNow.AddDays(-14);
                        var recentArticles = await db.Set<Article>()
                            .Where(a => a.PublishedAt >= sinceDate)
                            .ToListAsync(stoppingToken);

                        _logger.LogInformation("Found {Count} recent articles for {Continent}", recentArticles.Count, continent);

                        foreach (var article in recentArticles)
                        {
                            var dto = new ArticleDto
                            {
                                Id = article.Id,
                                Title = article.Title,
                                Content = article.Content,
                                Author = article.Author,
                                PublishedAt = article.PublishedAt,
                                Continent = article.Continent,
                                TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                            };

                            await _cacheService.SetArticleAsync(dto);
                        }

                        _logger.LogInformation("Cached {Count} articles in Redis for {Continent}", recentArticles.Count, continent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while refreshing article cache.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Article cache background service stopping...");
                    break;
                }

            }
        }
    }
}
