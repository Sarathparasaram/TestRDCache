using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;
using TestRDCache.Data;
using TestRDCache.Models;
using System;
using System.Text.Json;

namespace TestRDCache.Controllers
{
    // This block of commented code is used for validation purpose only - getting from Db vs Getting from Redis Cache

    //[Route("api/[controller]")]
    //[ApiController]
    //public class ProductController : ControllerBase
    //{


    //    private readonly AdventureWorksDbContext _context;

    //    public ProductController(AdventureWorksDbContext context)
    //    {
    //        _context = context;
    //    }

    //    [HttpGet]
    //    public async Task<IActionResult> GetAll()
    //    {
    //        var products = await _context.Products.ToListAsync();

    //        return Ok(products);
    //    }

    //}



    //This is used explicitely for Redis Cache demonstration - distributed cache.

    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AdventureWorksDbContext _context;
        private readonly IDistributedCache _cache;

        public ProductController(AdventureWorksDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cacheKey = "GET_ALL_PRODUCTS";
            List<Product> products = new List<Product>();

            // Get data from cache
            var cachedData = await _cache.GetAsync(cacheKey);
            if (cachedData != null)
            {
                // If data found in cache, encode and deserialize cached data
                var cachedDataString = Encoding.UTF8.GetString(cachedData);
                products = JsonConvert.DeserializeObject<List<Product>>(cachedDataString);
            }
            else
            {
                // If not found, then fetch data from database
                products = await _context.Products.ToListAsync();

                var cachedDataString = JsonConvert.SerializeObject(products);
                var newDataToCache = Encoding.UTF8.GetBytes(cachedDataString);

                // set cache options 
                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(2))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(1));

                // Add data in cache
                await _cache.SetAsync(cacheKey, newDataToCache, options);
            }

            return Ok(products);
        }


        [HttpPost("set-cache-key")]
        public IActionResult SetCache([FromBody] List<Product> products)
        {
            if (products == null)
            {
                return BadRequest("Product data is invalid.");
            }

            var cacheKey = "GET_ALL_PRODUCTS";

            try
            {
                var productsJson = JsonConvert.SerializeObject(products);

                var productsBytes = Encoding.UTF8.GetBytes(productsJson);

                var cacheEntryOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30), // Set an expiration time
                    SlidingExpiration = TimeSpan.FromMinutes(10) // Optional sliding expiration
                };

                // Update the cache with the new product data and cache options
                _cache.Set(cacheKey, productsBytes, cacheEntryOptions);

                // Retrieve the updated value from the cache
                byte[] cachedData = _cache.Get(cacheKey);

                if (cachedData != null)
                {
                    List<Product> updatedProducts = DeserializeFromBytes<List<Product>>(cachedData);

                    Console.WriteLine("Cache updated successfully.");
                    return Ok(new { CacheKey = cacheKey, UpdatedValue = updatedProducts });
                }
                else
                {
                    return NotFound("Cache item not found after update.");
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the cache: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(Product updatedProduct)
        {

            // invalidate the cache for the specific product
            string cacheKey = "Product_" + updatedProduct.ProductId;
            await _cache.RemoveAsync(cacheKey);

            return Ok(updatedProduct);
        }

        private T DeserializeFromBytes<T>(byte[] bytes)
        {
            if (bytes == null)
            {
                return default; 
            }

            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json);

        }

    }
}




