using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TestRDCache.Data;
using TestRDCache.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;

// This controller uses only In-Memory caching as an example or reference

namespace TestRDCache.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductControllerMemCache : ControllerBase
    {
        
      
            private readonly AdventureWorksDbContext _context;
            private readonly IMemoryCache _memcache;

            public ProductControllerMemCache(AdventureWorksDbContext context, IMemoryCache memcache)
            {
                _context = context;
                _memcache = memcache;
            }

            [HttpGet("products-list")]
            public async Task<IActionResult> GetAll()
            {
                var cacheKey = "GET_ALL_PRODUCTS";

                if (_memcache.TryGetValue(cacheKey, out List<Product> products))
                {
                    return Ok(products);
                }

                products = await _context.Products.ToListAsync();

                // Add data in cache
                _memcache.Set(cacheKey, products);

                return Ok(products);
            }

        [HttpPost("invalidate-cache-key")]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            if (product == null)
            {
                return BadRequest("Product data is invalid.");
            }

            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Invalidate the cache after adding a new product, so the next GetAll request will fetch fresh data.
                var cacheKey = "GET_ALL_PRODUCTS";
                _memcache.Remove(cacheKey);

                return CreatedAtAction("GetProduct", new { id = product.ProductId }, product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while adding the product: {ex.Message}");
            }
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
                // Update the cache with the new product data
                _memcache.Set(cacheKey, products);

                // Retrieve the updated value from the cache
                if (_memcache.TryGetValue(cacheKey, out List<Product> updatedProducts))
                {
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

        [HttpGet("Cash/{Id}")]    
        public IActionResult GetCacheById(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return BadRequest("Cache key is missing or invalid.");
            }

            if (_memcache.TryGetValue(cacheKey, out object cachedItem))
            {
                // Cache item found, return it
                return Ok(cachedItem);
            }
            else
            {
                return NotFound("Cache item not found.");
            }
        }




    }
}
