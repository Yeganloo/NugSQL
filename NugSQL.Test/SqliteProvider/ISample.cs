using NugSQL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NugSQL.Test.SqliteTest
{
    public interface ISample: IQueries
    {
        void create_tbl_allTypes();
        long create_allType(
            long my_int64,
            int my_int32,
            short my_int16,
            string my_string,
            byte[] my_bytes,
            bool my_bool,
            Jsonb my_json,
            float my_single,
            double my_double,
            BitArray my_bits,
            Guid my_guid,
            DateTime my_dateTime);
        void create_tbl_user();
        long create_user(string user_name, byte[] password, byte[] salt, Jsonb profile, short status);
        IEnumerable<User> get_users(string name);
    }
}