using System;
using Xunit;
using System.Reflection;
using System.IO;
using NugSQL.Providers;

namespace NugSQL.Test
{
    public class PostgresqlTypeTest
    {
        string cnn = "Host=localhost;Username=postgres;Password=123;Database=postgres";

        private string _QueryPath
        {
            get 
            {
                return $"{Path.GetDirectoryName(Assembly.GetAssembly(typeof(PostgresqlTest)).Location)}{Path.DirectorySeparatorChar}pg-queries";
            }
        }

        [Fact]
        public void CheckTypes()
        {
            var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new PgDatabaseProvider());
            var query = QuerBuilder.New<ISample>(cnn, typ);
            using(query.BeginTransaction())
            {
                query.create_schema_test();
                query.create_tbl_allTypes();
                query.create_allType(
                    long.MaxValue,
                    int.MaxValue,
                    short.MaxValue,
                    "Hello World",
                    new byte[] { 0, 1, 255 },
                    true,
                    @"{ ""Message"": ""This is a Json"" }",
                    (float)2.5,
                    double.MaxValue - 0.5,
                    new System.Collections.BitArray(new bool[]{false, false, true, false}),
                    Guid.NewGuid(),
                    DateTime.Now);
            }
        }

    }
}
