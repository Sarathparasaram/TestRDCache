using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;
using TestRDCache.Models;

namespace TestRDCache
{

    public class CacheManager
    {
        private readonly IDistributedCache _cache;

        public CacheManager(IDistributedCache cache)
        {
            _cache = cache;
        }

        // Event handler for data update events
        public void HandleDataUpdatedEvent(object sender, EventArgs e)
        {
            UpdateCache();
        }

        // Fetch data from the cache or data source
        public List<Product> GetProducts()
        {
            var cachedData = _cache.Get("products");

            if (cachedData != null)
            {
                return DeserializeFromBytes<List<Product>>(cachedData);
            }
            else
            {
                // If not in the cache, fetch data from the data source
                var data = FetchDataFromDataSource();

                // Store the data in the cache for future requests
                _cache.Set("products", SerializeToBytes(data));

                return data;
            }
        }

        // Update the cache with the latest data
        private void UpdateCache()
        {
            var data = FetchDataFromDataSource();
            _cache.Set("products", SerializeToBytes(data));
        }

        private byte[] SerializeToBytes<T>(T data)
        {
            string json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }

        private T DeserializeFromBytes<T>(byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private List<Product> FetchDataFromDataSource()
        {
            return new List<Product>();
        }
    }
}


