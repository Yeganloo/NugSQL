namespace NugSQL
{
    public struct RawQuery
    {
        public RawQuery(string name, string query, ResultTypes typ)
        {
            Query = query;
            Name = name;
            ResultType = typ;
        }
        public string Query;
        public ResultTypes ResultType;
        public string Name;
    }
}