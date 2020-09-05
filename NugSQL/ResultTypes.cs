namespace NugSQL
{
    public enum ResultTypes : byte
    {
        none = 0,
        affected = 1,
        scalar = 2,
        one = 3,
        many = 4,
    }
}