using System;
using Xunit;
using NugSQL;
using System.Reflection;
using System.IO;
using Npgsql;
using NugSQL.Providers;

namespace NugSQL.Test
{
    public class CompileTest
    {
        [Fact]
        public void Test1()
        {
            var typ =  QuerBuilder.Compile<ISample>(
                $"{Path.GetDirectoryName(Assembly.GetAssembly(typeof(CompileTest)).Location)}{Path.DirectorySeparatorChar}queries",
                new PgDatabaseProvider()
            );
            
            var cnn = "Host=localhost;Username=postgres;Password=123;Database=postgres";

            var query = QuerBuilder.New<ISample>(cnn, typ);
            Console.WriteLine("Nugsql -------------------");
            //Console.WriteLine("schema");
            //query.create_schema_test();
            //Console.WriteLine("table");
            //query.create_tbl_user();

            // Console.WriteLine("create user");
            // Console.WriteLine(query.create_user("admin", new byte[32], new byte[16], @"{ ""nikname"": ""sys_admin"" }", 1));
            // Console.WriteLine(query.create_user("amin", new byte[32], new byte[16], @"{ ""nikname"": ""amin"" }", 2));
            Console.WriteLine("get user");
            foreach (var res in query.get_users("a%"))
            {
                Console.WriteLine(res.id);
                Console.WriteLine(res.user_name);
                Console.WriteLine(res.profile);
                Console.WriteLine(res.status);
                Console.WriteLine(res.salt.Length);
                Console.WriteLine(res.password.Length);
                Console.WriteLine("-------------------");
            }
        }
    }
}
