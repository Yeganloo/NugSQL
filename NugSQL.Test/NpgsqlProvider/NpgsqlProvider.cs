using System;
using Xunit;
using System.Reflection;
using System.IO;
using NugSQL.Providers;

namespace NugSQL.Test.NpgsqlTest
{
    public class PostgresqlTest
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
        public void CompileFromFiles()
        {
            var typ =  QueryBuilder.Compile<ISample>(_QueryPath,new PgDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
        }

        [Fact]
        public void CompileFromResources()
        {
            var assembly = Assembly.GetAssembly(typeof(PostgresqlTest));
            var typ =  QueryBuilder.Compile<ISample>(assembly, new PgDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
        }

        [Fact]
        public void CreateScheemaTest()
        {
            var assembly = Assembly.GetAssembly(typeof(PostgresqlTest));
            var typ =  QueryBuilder.Compile<ISample>(assembly, new PgDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
            query.create_schema_test();
        }

        [Fact]
        public void CreateTableTest()
        {
            var typ =  QueryBuilder.Compile<ISample>(_QueryPath,new PgDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
            query.create_schema_test();
            query.create_tbl_user();
        }

        [Fact]
        public void TransactionTest()
        {
            var typ =  QueryBuilder.Compile<ISample>(_QueryPath,new PgDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
            query.create_schema_test();
            query.create_tbl_user();
            
            using(var tr = query.BeginTransaction())
            {
                query.create_user("u1", new byte[0], new byte[0], @"{ ""title"":""test"" }", 1);
                query.create_user("u2", new byte[0], new byte[0], @"{ ""title"":""test"" }", 1);
                tr.Commit();
            }
            using(var tr = query.BeginTransaction())
            {
                query.create_user("u3", new byte[0], new byte[0], @"{ ""title"":""test3"" }", 0);
                tr.Rollback();
            }

            foreach(var u in query.get_users("u%"))
            {
                Assert.True(u.user_name == "u1" || u.user_name == "u2");
            }
        }


        // TODO NestedTransaction
        // Npgsql dose not support nested transaction but SavePoints are totaly compatible by 
        // "using()" pattern. Should i transparently implient nested transactions using SavePoint?
        //[Fact]
        public void NestedTransactionTest()
        {
            var typ =  QueryBuilder.Compile<ISample>(_QueryPath,new PgDatabaseProvider());
            var query = QueryBuilder.New<ISample>(cnn, typ);
            query.create_schema_test();
            query.create_tbl_user();

            using(var tr1 = query.BeginTransaction())
            {
                query.create_user("u1", new byte[0], new byte[0], @"{ ""title"":""test"" }", 0);
                using(var tr2 = query.BeginTransaction())
                {
                    query.create_user("u2", new byte[0], new byte[0], @"{ ""title"":""test"" }", 0);
                    tr2.Commit();
                }
                using(var tr3 = query.BeginTransaction())
                {
                    query.create_user("u3", new byte[0], new byte[0], @"{ ""title"":""test3"" }", 0);
                    tr3.Rollback();
                }
                tr1.Commit();
            }

            foreach(var u in query.get_users("u%"))
            {
                Assert.True(u.user_name == "u1" || u.user_name == "u2");
            }
        }
    

    }
}
