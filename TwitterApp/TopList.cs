using Microsoft.Extensions.Configuration;

namespace TwitterApp
{
    class DecendingComparer<TKey> : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return y.CompareTo(x);
        }
    }
    public class TopList : ITopList
    {
        // Use a sorted list to keep the top ten together at one end of the list.
        private SortedList<int, HashSet<string>> _list = new SortedList<int, HashSet<string>>(new DecendingComparer<int>());

        // Use a dictionary to track counts of each strings.
        private Dictionary<string, int> _dictionary = new Dictionary<string, int>();

        private readonly IConfiguration _config;
        private readonly int _listSize;

        public TopList(IConfiguration config)
        {
            _config = config;
            _listSize = _config.GetValue<int>("hashtagListSize");
        }

        public List<KeyValuePair<string, int>> GetTopList()
        {
            // returns a list of <label, count>
            List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>(_listSize);

            var listEnuerator = _list.GetEnumerator();
            while (listEnuerator.MoveNext() && result.Count < _listSize)
            {
                int count = listEnuerator.Current.Key;
                var labelSet = listEnuerator.Current.Value;

                var labelSetEnumerator = labelSet.GetEnumerator();
                while (labelSetEnumerator.MoveNext() && result.Count < _listSize)
                {
                    result.Add(new KeyValuePair<string, int>(labelSetEnumerator.Current, count));
                }
            }
            return result;
        }
        public void Add(string label)
        {
            int previousCount = 0;
            int newCount = 1;
            // Using the dictionary, count the occurances of this label.
            if (_dictionary.ContainsKey(label))
            {
                previousCount = _dictionary[label];
                newCount = previousCount + 1;
            }
            _dictionary[label] = newCount;


            // Using the SortedList...
            // Remove the label from the previous count key
            if (_list.ContainsKey(previousCount))
            {
                HashSet<string> previousHashSet = _list[previousCount];
                previousHashSet.Remove(label);
            }

            // Add the label to the new count key
            if (_list.ContainsKey(newCount))
            {
                HashSet<string> newHashSet = _list[newCount];
                newHashSet.Add(label);
            }
            else
            {
                HashSet<string> newHashSet = new HashSet<string>();
                newHashSet.Add(label);
                _list.Add(newCount, newHashSet);
            }
        }
    }
}
