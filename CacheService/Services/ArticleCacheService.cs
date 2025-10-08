using CacheService.Dtos;
using Monitoring;
using System;

namespace CacheService.Services
{
    public class ArticleCacheService
    {
        private readonly RedisCacheService _redisCacheService;
        private const string KeyPrefix = "article:";

        public ArticleCacheService(RedisCacheService redisCacheService)
        {
            _redisCacheService = redisCacheService;
            MonitorService.Log.Information("ArticleCacheService initialized for {ServiceName}", MonitorService.ServiceName);
        }

        // Get from cache (continent)
        public async Task<ArticleDto?> GetArticleAsync(string continent, int id)
        {
            var key = $"{KeyPrefix}{continent}:{id}";
            MonitorService.RecordRequest();
            var article = await _redisCacheService.GetAsync<ArticleDto>(key);

            if (article != null)
            {
                MonitorService.RecordCacheHit();
                MonitorService.Log.Information("Cache HIT for {Key}", key);
                return article;
            }

            MonitorService.RecordCacheMiss();
            MonitorService.Log.Information("Cache MISS for {Key}", key);
            return null;
        }

        // Save to cache (used on cache miss or after DB update)
        public async Task SetArticleAsync(string continent, ArticleDto article, TimeSpan? expiry = null)
        {
            var key = $"{KeyPrefix}{continent}:{article.Id}";
            await _redisCacheService.SetAsync(key, article, expiry ?? TimeSpan.FromHours(1));
        }

        // Remove from cache (after delete)
        public async Task RemoveArticleAsync(string continent, int id)
        {
            var key = $"{KeyPrefix}{continent}:{id}";
            await _redisCacheService.RemoveAsync(key);
        }
    }
}
