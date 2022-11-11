using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;

namespace TwitterApp
{
    public record TweetIntervalRecord
    {
        public readonly int TweetsTotalCount;
        public readonly int TweetsThisIntervalCount;
        public readonly int TweetsPerSecond;

        public TweetIntervalRecord(TimeOnly previousIntervalTime, int previousIntervalTweetCount, int currentTotalTweetCount)
        {
            // Tweets per second is calculated on a per interval basis, not an overall basis.
            TimeOnly now = TimeOnly.FromDateTime(DateTime.Now);
            TimeSpan elapsedTimeThisInterval = now - previousIntervalTime;
            int seconds = elapsedTimeThisInterval.Seconds;
            if (seconds < 1)
            {
                seconds = 1;
            }
            this.TweetsTotalCount = currentTotalTweetCount;
            this.TweetsThisIntervalCount = TweetsTotalCount - previousIntervalTweetCount;
            this.TweetsPerSecond = TweetsThisIntervalCount / seconds;
        }
    }
    // This class manages the interface with the twitter services and collects statistics from it.
    internal class TwitterSamplingServiceClient : ITwitterSamplingServiceClient
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TwitterSamplingServiceClient> _logger;
        private readonly ITopList _topTenList;
        private readonly Tweetinvi.TwitterClient _tweetinviClient;

        private Tweetinvi.Streaming.V2.ISampleStreamV2? _sampleStream;

        private bool _asciiOnly = false;
        private int _previousIntervalTweetCount = 0;
        private TimeOnly _previousIntervalTime;
        private int _tweetCount = 0;


        public TwitterSamplingServiceClient(IConfiguration config, ILogger<TwitterSamplingServiceClient> logger, ITopList topTenList)
        {
            _config = config;
            _logger = logger;
            _topTenList = topTenList;

            _tweetinviClient = new Tweetinvi.TwitterClient(
                consumerKey: _config.GetValue<string>("Auth:APIKey"),
                consumerSecret: _config.GetValue<string>("Auth:APIKeySecret"),
                bearerToken: _config.GetValue<string>("Auth:BearerToken")
            );
            if (_config.GetValue<bool>("asc"))
            {
                _asciiOnly = true;
            }
        }

        public void StartListener(CancellationToken ct)
        {
            _sampleStream = _tweetinviClient.StreamsV2.CreateSampleStream();
            _sampleStream.TweetReceived += (sender, args) =>
            {
                //Console.WriteLine(Regex.Replace(args.Tweet.Text, @"[^\u0000-\u007F]+", string.Empty));
                var hashtags = args.Tweet.Entities.Hashtags;
                if (hashtags != null)
                {
                    foreach (var hashtag in hashtags)
                    {
                        if (hashtag != null)
                        {
                            string hashTagString = hashtag.Tag;
                            if (_asciiOnly)
                            {
                                hashTagString = Regex.Replace(hashTagString, @"[^\u0000-\u007F]+", string.Empty);
                            }
                            if (string.IsNullOrEmpty(hashTagString) == false)
                            {
                                ProcessHashtag(hashTagString);
                            }
                        }
                    }
                }
                _tweetCount++;
            };

            Task streamTask = _sampleStream.StartAsync();
            try
            {
                while (ct.IsCancellationRequested == false)
                {
                    // Wait a half second and check if we're cancelled
                    streamTask.Wait(500);
                }
            }
            catch(AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    //Console.WriteLine(ex.Message);
                    _logger.LogError(ex.Message);
                }
            }
        }

        public TweetIntervalRecord GetIntervalRecord()
        {
            var intervalStats = new TweetIntervalRecord(_previousIntervalTime, _previousIntervalTweetCount, _tweetCount);

            _previousIntervalTweetCount = _tweetCount;
            _previousIntervalTime = TimeOnly.FromDateTime(DateTime.Now);

            return intervalStats;
        }

        public List<KeyValuePair<string, int>> GetTopTen()
        {
            return _topTenList.GetTopTen();
        }

        private void ProcessHashtag(string hashtag)
        {
            //Console.WriteLine("Hashtag: [{0}]", hashtag);
            _topTenList.Add(hashtag);
        }
    }
}
