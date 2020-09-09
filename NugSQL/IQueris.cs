namespace NugSQL
{
    using System.Data;

    public interface IQueries
    {
        IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    }
}