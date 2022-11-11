namespace TwitterApp
{
    public interface ITwitterAppHostingService
    {
        public bool Run();
        public bool IsValidUsage();
        public string GetUsage();
    }
}
