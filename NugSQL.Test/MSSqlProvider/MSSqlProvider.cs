using NugSQL.Providers;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Reflection;
using Xunit;


namespace NugSQL.Test.MSSqlTest
{
  public class MSSQLProvider
  {
    string cnn = @"Server=.;Database=db;User Id=user;Password=pass;";

    private string _QueryPath
    {
      get 
      {
        return $"{Path.GetDirectoryName(Assembly.GetAssembly(typeof(MSSQLProvider)).Location)}{Path.DirectorySeparatorChar}mssql-queries";
      }
    }

    [Fact]
    public void Setup()
    {
      var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new MSSqlDatabaseProvider());
      var query = QuerBuilder.New<ISample>(cnn, typ);
    }

    [Fact]
    public void Create_table()
    {
      var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new MSSqlDatabaseProvider());
      var query = QuerBuilder.New<ISample>(cnn, typ);
      using(query.BeginTransaction())
      {
        query.create_schema_test();
        query.create_tbl_user();
      }
    }

    [Fact]
    public void Transaction_Test()
    {
      var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new MSSqlDatabaseProvider());
      var query = QuerBuilder.New<ISample>(cnn, typ);
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
        query.create_user("u3", new byte[0], new byte[0], @"{ ""title"":""test3"" }", 1);
         tr.Rollback();
      }
      foreach(var u in query.get_users("u%"))
      {
        Assert.True(u.user_name == "u1" || u.user_name == "u2");
      }
    }

  }
}