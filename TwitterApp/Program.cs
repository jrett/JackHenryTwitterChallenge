using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TwitterApp
{
    internal class Program
    {
        // An assumption was made as to the lifetime of this application process runtime. It is assumed to be run for relatively short periods of time
        // because the implementation of top ten calculations doesn't account for growth over long periods of time.
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            ILogger? logger = null;

            try
            {
                var provider = Startup.ConfigureServices(args);

                logger = provider.GetService<ILogger<Program>>();
                if (logger == null)
                {
                    throw new Exception("Logger not configured.");
                }
                logger.LogInformation("Starting up.");

                var worker = provider.GetService<ITwitterAppHostingService>();
                if (worker == null)
                {
                    throw new Exception("Twitter application host not configured.");
                }
                worker.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                logger?.LogCritical(e, "Unexpected Error");
                return 1;
            }
            logger?.LogInformation("Finished");
            return 0;
        }

    }
}