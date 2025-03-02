using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Authentication.Infrastructure.Services
{
    /// <summary>
    /// Provides a generic distributed caching service with flexible type handling
    /// </summary>
    public class DistributedCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        public DistributedCacheService(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Retrieves a cached value by key
        /// </summary>
        public async Task<T>? GetAsync<T>(string key)
        {
            var data = await _cache.GetAsync(key);
            if (data == null)
                return default;
            
            var jsonString = Encoding.UTF8.GetString(data);
            
            // Handle different type scenarios
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(jsonString, typeof(T));
            }
            
            return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);
        }

        /// <summary>
        /// Sets a value in the distributed cache
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
        {
            string jsonString;
            
            // Handle different type scenarios
            if (value == null)
            {
                jsonString = "null";
            }
            else if (value.GetType().IsPrimitive || value is string)
            {
                jsonString = value.ToString();
            }
            else
            {
                jsonString = JsonSerializer.Serialize(value, _jsonOptions);
            }
            
            var data = Encoding.UTF8.GetBytes(jsonString);
            
            var options = new DistributedCacheEntryOptions();
            
            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }
            
            await _cache.SetAsync(key, data, options);
        }

        /// <summary>
        /// Removes a value from the cache
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        /// <summary>
        /// Refreshes the expiration of a cached item
        /// </summary>
        public async Task RefreshAsync(string key)
        {
            await _cache.RefreshAsync(key);
        }
    }
}