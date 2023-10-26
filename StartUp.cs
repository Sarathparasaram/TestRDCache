using Microsoft.EntityFrameworkCore;
using TestRDCache.Data;
using System.Configuration;
using Microsoft.Extensions.Configuration;


namespace TestRDCache
{
    public class StartUp
    {
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddControllersWithViews();
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .Build();
            services.AddSingleton<IConfiguration>(configuration);


            services.AddDbContext<AdventureWorksDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddMemoryCache();


            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["RedisCacheServerUrl"];
            });
        }


    }
}
