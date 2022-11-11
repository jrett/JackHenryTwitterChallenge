namespace TwitterApp
{
    public interface ITopList
    {
        void Add(string label);
        List<KeyValuePair<string, int>> GetTopTen();
    }
}