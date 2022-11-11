using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterApp
{
    // This class provides the central processing for the application.
    // It manages configuration validation, usage info and interval timing, as well as presentation.
    internal class TwitterAppHostingService: ITwitterAppHostingService
    {
        private readonly IConfiguration? _config;
        private readonly ILogger<TwitterAppHostingService>? _logger;
        private readonly ITwitterSamplingServiceClient? _twitterService;

        private System.Timers.Timer? _intervalTimer;

        public TwitterAppHostingService(IConfiguration? config, ILogger<TwitterAppHostingService>? logger, ITwitterSamplingServiceClient? twitterService)
        {
            _config = config;
            _logger = logger;
            _twitterService = twitterService;
        }
        public bool IsValidUsage()
        {
            if (_config == null)
            {
                _logger?.LogError("Configuration is null");
                return false;
            }
            // Check config and command line arguments.
            if ((IsValidConfigItem("Auth:APIKey") &&
                IsValidConfigItem("Auth:APIKeySecret") &&
                IsValidConfigItem("Auth:BearerToken")) == false) {
                return false;
            }

            int interval = _config.GetValue<int>("int");
            if (interval < 1)
            {
                _logger.LogError("The \"int\" parameter is required and must be greater than 0.");
                return false;
            }

            return true;
        }

        private bool IsValidConfigItem(string itemKey)
        {
            if (string.IsNullOrEmpty(_config.GetValue<string>(itemKey)))
            {
                _logger?.LogError($"Configuration is missing required value for: [{itemKey}]");
                return false;
            }
            return true;
        }

        public string GetUsage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName} int=reportingInterval [asc=true/false]");
            sb.AppendLine();
            sb.AppendLine(" int   Number; The reporting interval in seconds.");
            sb.AppendLine(" asc   true/false; Ascii character set only. Optionally filter out non ascii characters.");
            sb.AppendLine();
            sb.AppendLine("Twitter Sample Stream API will be observed and a running total of tweets");
            sb.AppendLine("(about 1% of all tweets) will be counted. Additionally the top ten hashtags");
            sb.AppendLine("will be calculated and reported.");
            sb.AppendLine("The reporting of this information will occur at the specified reporting interval.");
            sb.AppendLine();
            sb.AppendLine("If the a flag is specified, non ASCII characters will be filtered out of the input");
            sb.AppendLine("hashtag data and excluded from the top ten list.");
            sb.AppendLine();
            sb.AppendLine($"Example Usage:  {System.AppDomain.CurrentDomain.FriendlyName} int=2 asc=true");
            sb.AppendLine("The above example will report every 2 seconds the total number of tweets, and the");
            sb.AppendLine("top ten hashtags. Hashtags that are comprised exclusively from non ASCII characters");
            sb.AppendLine("will be excluded from consideration.");
            sb.AppendLine();

            return sb.ToString();
        }

        public bool Run()
        {
            if (IsValidUsage() == false)
            {
                _logger?.LogInformation(GetUsage());
                _logger?.LogError("Incorrect usage.");
                return false;
            }

            if (_twitterService == null)
            {
                return false;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var twitterListerTask = Task.Run(() => _twitterService.StartListener(cancellationTokenSource.Token));

            // Setup interval timer
            int intervalSeconds = _config.GetValue<int>("int");
            _logger?.LogInformation($"Starting timer for [{intervalSeconds}] second intervals.");

            int intervalMilliseconds = intervalSeconds * 1000;
            _intervalTimer = new System.Timers.Timer(intervalMilliseconds);
            _intervalTimer.Elapsed += IntervalTimer_Elapsed;
            _intervalTimer.AutoReset = true;
            _intervalTimer.Enabled = true;


            // Wait here for the user to tell us that we're done.
            Console.ReadLine();
            _intervalTimer.Enabled = false;
            _logger.LogInformation("User requested cancellation of processing. Reqeusting cancellation.");
            cancellationTokenSource.Cancel();
            twitterListerTask.Wait(); // Make sure the task completes before shutting down.
            _logger.LogInformation("Processing has concluded.");

            PresentIntervalData();

            return true;
        }

        private void IntervalTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            PresentIntervalData();
        }
        private void PresentIntervalData()
        {
            if (_twitterService != null)
            {
                TweetIntervalRecord intervalRecord = _twitterService.GetIntervalRecord();
                var topTen = _twitterService.GetTopTen();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("Tweet Stats");
                sb.AppendLine();
                sb.AppendLine($"  Total Tweets:               [{intervalRecord.TweetsTotalCount}]");
                sb.AppendLine($"  Total Tweets this interval: [{intervalRecord.TweetsThisIntervalCount}]");
                sb.AppendLine($"  Tweets/Second:              [{intervalRecord.TweetsPerSecond}]");
                sb.AppendLine();
                sb.AppendLine("Top Ten Hashtags");
                sb.AppendLine();
                foreach (var hashtagStat in topTen)
                {
                    sb.AppendLine($"  There has been [{hashtagStat.Value}] occurances of the following HashTag: [{hashtagStat.Key}].");
                }
                _logger.LogInformation(sb.ToString());
            }
        }
    }
}
