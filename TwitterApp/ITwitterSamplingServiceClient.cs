namespace TwitterApp
{
    internal interface ITwitterSamplingServiceClient
    {
        void StartListener(CancellationToken ct);
        public TweetIntervalRecord GetIntervalRecord();
        public List<KeyValuePair<string, int>> GetTopList();
    }
}