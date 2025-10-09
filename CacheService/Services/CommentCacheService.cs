using Shared.Models;
using Shared;
using Prometheus;
using System.Collections.Concurrent;

namespace CacheService.Services
{
    public class CommentCacheService
    {
        private readonly RedisCacheService _redisCacheService;
        private const string KeyPrefix = "comment:";
        private const int MaxArticles = 30;     // TODO Change naming to comment

        // key = last access timestamp
        private readonly ConcurrentDictionary<string, DateTime> _accessMap = new();

        public CommentCacheService(RedisCacheService redisCacheService)
        {
            _redisCacheService = redisCacheService;
            MonitorService.Log.Information("CommentCacheService initialized for {ServiceName}", MonitorService.ServiceName);
        }

        // Try read from cache
        public async Task<List<CommentDto>?> GetCommentsAsync(string continent, int articleId)
        {
            using var activity = MonitorService.ActivitySource.StartActivity("GetCommentsFromCache");

            var key = $"{KeyPrefix}{continent}{articleId}";
            MonitorService.RecordRequest();

            var cached = await _redisCacheService.GetAsync<List<CommentDto>>(key);
            if (cached != null)
            {
                MonitorService.RecordCacheHit();
                Touch(key); // updating access time for LRU
                activity?.SetTag("cache.result", "hit");
                return cached;
            }

            MonitorService.RecordCacheMiss();
            activity?.SetTag("cache.result", "miss");
            return null;
        }

        // Add to cache after a miss
        public async Task SetCommentsAsync(string continent, int articleId, List<CommentDto> comments)
        {
            using var activity = MonitorService.ActivitySource.StartActivity("SetCommentsInCache");
            var key = $"{KeyPrefix}{continent}{articleId}";
            await _redisCacheService.SetAsync(key, comments, TimeSpan.FromHours(1));

            Touch(key);
            EvictIfNeeded();

            activity?.SetTag("cache.key", key);
            activity?.SetTag("cache.size", comments.Count);
        }

        // Optional manual removal
        public async Task RemoveCommentsAsync(string continent, int articleId)
        {
            var key = $"{KeyPrefix}{continent}{articleId}";
            _accessMap.TryRemove(key, out _);
            await _redisCacheService.RemoveAsync(key);
        }

        // --- Internal helpers ---

        private void Touch(string key)
        {
            _accessMap[key] = DateTime.UtcNow;
        }

        private async void EvictIfNeeded()
        {
            if (_accessMap.Count <= MaxArticles)
                return;

            // find oldest (least recently used)
            var oldest = _accessMap.OrderBy(kv => kv.Value).First().Key;
            _accessMap.TryRemove(oldest, out _);
            await _redisCacheService.RemoveAsync(oldest);
            MonitorService.Log.Information("Evicted LRU cached article: {Key}", oldest);
        }
    }
}
