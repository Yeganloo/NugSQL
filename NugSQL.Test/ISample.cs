using NugSQL;
using System.Collections.Generic;

namespace NugSQL.Test
{
    public interface ISample: IQueries
    {
        IEnumerable<Output> get_users(string name);
    }
}