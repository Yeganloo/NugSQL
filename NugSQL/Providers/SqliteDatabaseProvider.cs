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
            var ass = "Microsoft.Data.Sqlite.SqliteFactory, Microsoft.Data.Sqlite";
            _factory = GetFactory(ass, nameof(SqliteDatabaseProvider), ass);
            if(DbTypeProp == null)
                DbTypeProp = _factory.CreateParameter().GetType().GetProperty("SqliteType");
        }

        public override bool NeedTypeConversion(Type typ)
        {
            return typ == typeof(Json) || typ == typeof(Jsonb);
        }

        public static DbParameter MappParameter(DbParameter param, Jsonb value)
        {
            param.Value = (string)value;
            DbTypeProp.SetValue(param, Enum.Parse(DbTypeProp.PropertyType, "Text"));
            return param;
        }

        public static DbParameter MappParameter(DbParameter param, Json value)
        {
            param.Value = (string)value;
            DbTypeProp.SetValue(param, Enum.Parse(DbTypeProp.PropertyType, "Text"));
            return param;
        }
        
    }
}