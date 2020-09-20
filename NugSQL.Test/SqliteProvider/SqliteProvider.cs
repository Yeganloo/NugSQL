using Microsoft.Data.Sqlite;
using NugSQL.Providers;
using System.IO;
using System.Reflection;
using Xunit;


namespace NugSQL.Test
{
  public class SqliteProvider
  {
    string cnn = "Data Source=:memory:";

    private string _QueryPath
    {
      get 
      {
        return $"{Path.GetDirectoryName(Assembly.GetAssembly(typeof(SqliteProvider)).Location)}{Path.DirectorySeparatorChar}sqlite-queries";
      }
    }

    //[Fact]
    public void Setup()
    {
      var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new SqliteDatabaseProvider());
      var query = QuerBuilder.New<ISample>(cnn, typ);
    }

    //[Fact]
    public void Create_table()
    {
      var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new SqliteDatabaseProvider());
      var query = QuerBuilder.New<ISample>(cnn, typ);
      query.create_tbl_user();
    }

    //[Fact]
    public void Create_User()
    {
      var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new SqliteDatabaseProvider());
      var query = QuerBuilder.New<ISample>(cnn, typ);
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
    }

    //[Fact]
    public void Get_User()
    {
      var typ =  QuerBuilder.Compile<ISample>(_QueryPath,new SqliteDatabaseProvider());
      var query = QuerBuilder.New<ISample>(cnn, typ);
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