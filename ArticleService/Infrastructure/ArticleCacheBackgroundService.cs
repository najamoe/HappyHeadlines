using CacheService.Dtos;
using CacheService.Services;
using ArticleService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ArticleService.Infrastructure;

namespace ArticleService.Infrastructure
{
    public class ArticleCacheBackgroundService : BackgroundService
    {
        private readonly ILogger<ArticleCacheBackgroundService> _logger;
        private readonly ArticleCacheService _cacheService;

        // Inject all DB contexts
        private readonly AfricaDbContext _africa;
        private readonly AsiaDbContext _asia;
        private readonly EuropeDbContext _europe;
        private readonly NorthAmericaDbContext _northAmerica;
        private readonly SouthAmericaDbContext _southAmerica;
        private readonly OceaniaDbContext _oceania;
        private readonly AntarcticaDbContext _antarctica;
        private readonly GlobalDbContext _global;

        public ArticleCacheBackgroundService(
            ILogger<ArticleCacheBackgroundService> logger,
            ArticleCacheService cacheService,
            AfricaDbContext africa,
            AsiaDbContext asia,
            EuropeDbContext europe,
            NorthAmericaDbContext northAmerica,
            SouthAmericaDbContext southAmerica,
            OceaniaDbContext oceania,
            AntarcticaDbContext antarctica,
            GlobalDbContext global)
        {
            _logger = logger;
            _cacheService = cacheService;
            _africa = africa;
            _asia = asia;
            _europe = europe;
            _northAmerica = northAmerica;
            _southAmerica = southAmerica;
            _oceania = oceania;
            _antarctica = antarctica;
            _global = global;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Article cache background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Refreshing article cache for all continents...");

                try
                {
                    var continentContexts = new Dictionary<string, DbContext>
                    {
                        { "africa", _africa },
                        { "asia", _asia },
                        { "europe", _europe },
                        { "northamerica", _northAmerica },
                        { "southamerica", _southAmerica },
                        { "oceania", _oceania },
                        { "antarctica", _antarctica },
                        { "global", _global }
                    };

                    foreach (var kvp in continentContexts) // Key: continent name, Value: DbContext
                    {
                        var continent = kvp.Key;
                        var db = kvp.Value;

                        _logger.LogInformation("Refreshing cache for continent: {Continent}", continent);

                        var sinceDate = DateTime.UtcNow.AddDays(-14);
                        var recentArticles = await db.Set<ArticleService.Models.Article>()
                                                     .Where(a => a.PublishedAt >= sinceDate)
                                                     .ToListAsync(stoppingToken);

                        foreach (var article in recentArticles)
                        {
                            var dto = new ArticleDto
                            {
                                Id = article.Id.ToString(),
                                Title = article.Title,
                                Content = article.Content,
                                Author = article.Author,
                                PublishedAt = article.PublishedAt
                            };

                            await _cacheService.SetArticleAsync(continent, dto);
                        }

                        _logger.LogInformation("Cached {Count} articles for {Continent}", recentArticles.Count, continent);
                    }

                    _logger.LogInformation("Article cache refresh completed for all continents.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while refreshing article cache.");
                }

                // Wait an hour before the next refresh
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
