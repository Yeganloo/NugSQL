namespace NugSQL.Providers
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    public class PgDatabaseProvider : DatabaseProvider
    {
        private static PropertyInfo DbTypeProp;
        private static readonly DbProviderFactory _factory;

        public override DbProviderFactory Factory => _factory;


        static PgDatabaseProvider()
        {
            var ass = "Npgsql.NpgsqlFactory, Npgsql";
            _factory = GetFactory(nameof(PgDatabaseProvider), ass);
            if(DbTypeProp == null)
                DbTypeProp = _factory.CreateParameter().GetType().GetProperty("NpgsqlDbType");
        }

        public override bool NeedTypeConversion(Type typ)
        {
            return (typ == typeof(Json) || typ == typeof(Jsonb));
        }

        public static DbParameter MappParameter(DbParameter param, Jsonb value)
        {
            param.Value = (string)value;
            DbTypeProp.SetValue(param, Enum.Parse(DbTypeProp.PropertyType, "Jsonb"));
            return param;
        }

        public static DbParameter MappParameter(DbParameter param, Json value)
        {
            param.Value = (string)value;
            DbTypeProp.SetValue(param, Enum.Parse(DbTypeProp.PropertyType, "Json"));
            return param;
        }

        public static DbParameter MappParameter(DbParameter parameter, object obj, DbType dbtype)
        {
            parameter.Value = obj;
            parameter.DbType = dbtype;
            return parameter;
        }

    }
}