using CacheService.Dtos;
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
        }

        // Get from cache (continent)
        public async Task<ArticleDto?> GetArticleAsync(string continent, string id)
        {
            var key = $"{KeyPrefix}{continent}:{id}";
            return await _redisCacheService.GetAsync<ArticleDto>(key);
        }

        // Save to cache (used on cache miss or after DB update)
        public async Task SetArticleAsync(string continent, ArticleDto article, TimeSpan? expiry = null)
        {
            var key = $"{KeyPrefix}{continent}:{article.Id}";
            await _redisCacheService.SetAsync(key, article, expiry ?? TimeSpan.FromHours(1));
        }

        // Remove from cache (after delete)
        public async Task RemoveArticleAsync(string continent, string id)
        {
            var key = $"{KeyPrefix}{continent}:{id}";
            await _redisCacheService.RemoveAsync(key);
        }
    }
}
