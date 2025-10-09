using Shared.Models;
using Shared;

namespace CacheService.Services
{
    public class ArticleCacheService
    {
        private readonly RedisCacheService _redis;
        private const string KeyPrefix = "article:global:";

        public ArticleCacheService(RedisCacheService redis)
        {
            _redis = redis;
            MonitorService.Log.Information("ArticleCacheService initialized for {Service}", MonitorService.ServiceName);
        }

        public async Task SetArticleAsync(ArticleDto article, TimeSpan? expiry = null)
        {
            if (article == null) return;
            var key = $"{KeyPrefix}{article.Id}";
            await _redis.SetAsync(key, article, expiry ?? TimeSpan.FromHours(1));
            MonitorService.Log.Information("Cached global article {Id}", article.Id);
        }

        public async Task<ArticleDto?> GetArticleAsync(int id)
        {
            var key = $"{KeyPrefix}{id}";
            MonitorService.RecordRequest();

            var article = await _redis.GetAsync<ArticleDto>(key);
            if (article == null)
            {
                MonitorService.RecordCacheMiss();
                MonitorService.Log.Information("Cache MISS for {Key}", key);
                return null;
            }

            MonitorService.RecordCacheHit();
            MonitorService.Log.Information("Cache HIT for {Key}", key);
            return article;
        }

        public async Task RemoveArticleAsync(int id)
        {
            var key = $"{KeyPrefix}{id}";
            await _redis.RemoveAsync(key);
            MonitorService.Log.Information("Removed cached article {Key}", key);
        }
    }
}
