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
            foreach (var res in query.get_users("po%"))
            {
                Console.WriteLine(res.name);
                Console.WriteLine(res.count);
            }
            var q = @"select  1+1 as ""count"", u.name
                from user u
                where u.name like @name";
            
            var db = new PetaPoco.Database(cnn, "Npgsql");
            var pres = db.Single<Output>(q, new { name="po%" });
            Console.WriteLine("PetaPoco -------------------");
            Console.WriteLine(pres.name);
            Console.WriteLine(pres.count);

        }
    }
}
