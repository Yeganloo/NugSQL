using NugSQL;
using System.Collections.Generic;

namespace NugSQL.Test
{
    public interface ISample: IQueries
    {
        void create_tbl_user();
        void create_schema_test();
        int create_user(string user_name, byte[] password, byte[] salt, Jsonb profile, short status);
        IEnumerable<User> get_users(string name);
    }
}