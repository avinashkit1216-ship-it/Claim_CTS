// CacheService.cs - Distributed Caching Service (Redis/Memory)
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ClaimSubmission.API.Services
{
    /// <summary>
    /// Interface for distributed caching
    /// Supports both Redis and in-memory implementations
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
    }

    /// <summary>
    /// Implementation using distributed cache (Redis)
    /// </summary>
    public class DistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Get value from cache
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                if (value == null)
                    return default;

                return JsonSerializer.Deserialize<T>(value, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error retrieving cache key: {key}");
                return default;
            }
        }

        /// <summary>
        /// Get from cache or create using factory
        /// Implements cache-aside pattern
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                // Try to get from cache
                var cached = await GetAsync<T>(key);
                if (cached != null)
                {
                    _logger.LogDebug($"Cache hit for key: {key}");
                    return cached;
                }

                // Create new value
                _logger.LogDebug($"Cache miss for key: {key}");
                var value = await factory();

                // Store in cache
                await SetAsync(key, value, expiration ?? TimeSpan.FromMinutes(5));

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetOrCreateAsync for key: {key}");
                // Return fresh data on cache failure
                return await factory();
            }
        }

        /// <summary>
        /// Set value in cache
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
                };

                await _cache.SetStringAsync(key, json, options);
                _logger.LogDebug($"Set cache key: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error setting cache key: {key}");
            }
        }

        /// <summary>
        /// Remove specific key from cache
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug($"Removed cache key: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error removing cache key: {key}");
            }
        }

        /// <summary>
        /// Remove all cache keys matching prefix
        /// Useful for cache invalidation
        /// </summary>
        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                // Note: DistributedCache doesn't support pattern matching
                // In production with Redis, use Iserver.Keys() with StackExchange.Redis
                _logger.LogDebug($"RemoveByPrefixAsync called for prefix: {prefix} - implement in Redis provider");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error removing cache prefix: {prefix}");
            }
        }
    }
}
