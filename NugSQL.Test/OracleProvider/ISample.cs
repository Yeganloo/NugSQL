using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

namespace NugSQL.Test.OracleTest
{
    public interface ISample : IQueries
    {
        void create_tbl_allTypes();
        void create_tbl_user();
        int create_user(int id, string user_name, string password, string salt, string profile, short status);
        IEnumerable<oraUser> get_users(string name);
        IEnumerable<oraUser> get_users_by_id(OracleParameter ids);
        int? NullResult();
        void DeleteTestUsers();
    }
}