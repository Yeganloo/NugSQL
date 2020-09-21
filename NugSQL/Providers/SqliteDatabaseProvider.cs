namespace NugSQL.Providers
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    public class SqliteDatabaseProvider : DatabaseProvider
    {
        private static PropertyInfo DbTypeProp;
        private static readonly DbProviderFactory _factory;

        public override DbProviderFactory Factory => _factory;

        static SqliteDatabaseProvider()
        {
            _factory = GetFactory(
                nameof(SqliteDatabaseProvider),
                "System.Data.SQLite.SQLiteFactory, System.Data.SQLite",
                "Microsoft.Data.Sqlite.SqliteFactory, Microsoft.Data.Sqlite");
            if(DbTypeProp == null)
                DbTypeProp = _factory.CreateParameter().GetType().GetProperty("SqliteType");
        }

        public override bool NeedTypeConversion(Type typ)
        {
            return false;
        }
        
    }
}