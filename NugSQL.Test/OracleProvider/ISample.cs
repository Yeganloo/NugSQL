using NugSQL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NugSQL.Test.OracleTest
{
    public interface ISample : IQueries
    {
        void create_tbl_allTypes();
        void create_tbl_user();
        int create_user(int id, string user_name, string password, string salt, string profile, short status);
        IEnumerable<User> get_users(string name);
    }
}