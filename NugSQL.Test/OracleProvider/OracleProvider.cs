using NugSQL.Providers;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;
using Oracle.ManagedDataAccess.Client;

namespace NugSQL.Test.OracleTest
{
    public class OracleProvider
    {
        string cnn = @"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.2.30)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=otherdb)));User Id=oms;Password=oms;";

        private string _QueryPath
        {
            get
            {
                return $"{Path.GetDirectoryName(Assembly.GetAssembly(typeof(OracleProvider)).Location)}{Path.DirectorySeparatorChar}Oracle-queries";
            }
        }

        [Fact]
        public void Setup()
        {
            var typ = QueryBuilder.Compile<ISample>(_QueryPath, new OracleDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
        }

        [Fact]
        public void Create_table()
        {
            var typ = QueryBuilder.Compile<ISample>(_QueryPath, new OracleDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
            using (query.BeginTransaction())
            {
                query.create_tbl_user();
            }
        }

        [Fact]
        public void Transaction_Test()
        {
            var typ = QueryBuilder.Compile<ISample>(_QueryPath, new OracleDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
            //query.create_tbl_user();

            using (var tr = query.BeginTransaction())
            {
                query.DeleteTestUsers();
                tr.Commit();
            }
            using (var tr = query.BeginTransaction())
            {
                //BUG string.empty is translating to null!
                query.create_user(-1, "u1", " ", " ", @"{ ""title"":""test"" }", 1);
                query.create_user(-2, "u2", " ", " ", @"{ ""title"":""test"" }", 1);
                tr.Commit();
            }
            using (var tr = query.BeginTransaction())
            {
                query.create_user(-3, "u3", " ", " ", @"{ ""title"":""test3"" }", 1);
                tr.Rollback();
            }
            var qr = new List<oraUser>(query.get_users("u%"));
            Assert.True(qr.Count() > 1);
            foreach (var u in qr)
            {
                Assert.True(u.user_name == "u1" || u.user_name == "u2");
            }
        }

        [Fact]
        public void List_Param_Test()
        {
            var typ = QueryBuilder.Compile<ISample>(_QueryPath, new OracleDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);

            query.DeleteTestUsers();
            using (var tr = query.BeginTransaction())
            {
                //BUG string.empty is translating to null!
                query.create_user(-1, "u1", " ", " ", @"{ ""title"":""test"" }", 1);
                query.create_user(-2, "u2", " ", " ", @"{ ""title"":""test"" }", 1);
                tr.Commit();
            }

            var qr = new List<oraUser>(query.get_users_by_id(
                new OracleParameter{
                    OracleDbType = OracleDbType.Int32,
                    CollectionType = OracleCollectionType.PLSQLAssociativeArray,
                    ParameterName = ":ids",
                    Value = new int[2] { -1, -2 },
                    Size = 2
                }));
            Assert.True(qr.Count() == 2);
        }

        [Fact]
        public void Null_Result()
        {
            var typ = QueryBuilder.Compile<ISample>(_QueryPath, new OracleDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
            Assert.True(!query.NullResult().HasValue);
        }

    }
}