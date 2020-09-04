namespace NugSQL
{
    using System.Data;

    public interface IQueries
    {
        void BeginTransaction(IsolationLevel isolationLevel);
        void RollBack();
        void Commite();
    }
}