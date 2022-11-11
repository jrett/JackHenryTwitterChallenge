using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TwitterApp
{
    internal class Startup
    {
        internal static IServiceProvider ConfigureServices(string[] args)
        {
            // Setup application configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false)
                .AddCommandLine(args);
            IConfiguration config = configBuilder.Build();

            // Add configuration support
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(config);

            // Add main application hosting service
            services.AddSingleton<ITwitterAppHostingService, TwitterAppHostingService>();

            // Add main twitter sample processing service
            services.AddTransient<ITwitterSamplingServiceClient, TwitterSamplingServiceClient>();

            // Add top ten list processor
            services.AddSingleton<ITopList, TopList>();

            // Add some logging support
            services.AddLogging(builder =>
            {
                builder.AddSerilog(new LoggerConfiguration()
                    .MinimumLevel.Information()
                    //.WriteTo.File(path: "Logs\\log.txt")
                    .WriteTo.Console()
                    .CreateLogger());
            });

            return services.BuildServiceProvider();
        }
    }
}
