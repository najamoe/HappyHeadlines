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
        private readonly IServiceProvider _serviceProvider;

        public ArticleCacheBackgroundService(
            ILogger<ArticleCacheBackgroundService> logger,
            ArticleCacheService cacheService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _cacheService = cacheService;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Article cache background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Refreshing article cache for Global database...");

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<GlobalDbContext>();

                    var sinceDate = DateTime.UtcNow.AddDays(-14);
                    var recentArticles = await dbContext.Set<GlobalArticle>()
                        .Where(a => a.PublishedAt >= sinceDate)
                        .ToListAsync(stoppingToken);

                    _logger.LogInformation("Found {Count} recent GlobalArticles to cache.", recentArticles.Count);

                    foreach (var article in recentArticles)
                    {
                        var dto = new ArticleDto
                        {
                            Id = article.Id,
                            Title = article.Title,
                            Content = article.Content,
                            Author = article.Author,
                            PublishedAt = article.PublishedAt,
                            Continent = article.SourceContinent ?? "Global",
                            SourceArticleId = article.SourceArticleId,
                            TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                        };

                        await _cacheService.SetArticleAsync(dto);
                    }

                    _logger.LogInformation("Cached {Count} GlobalArticles in Redis.", recentArticles.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while refreshing global article cache.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
