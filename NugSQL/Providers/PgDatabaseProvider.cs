namespace NugSQL.Providers
{
    using System;
    using System.Data.Common;
    using System.Reflection;
    public class PgDatabaseProvider : DatabaseProvider
    {
        private static PropertyInfo DbTypeProp;
        private static readonly DbProviderFactory _factory;

        public override DbProviderFactory Factory => _factory;


        static PgDatabaseProvider()
        {
            _factory = GetFactory(nameof(PgDatabaseProvider), "Npgsql.NpgsqlFactory, Npgsql");
            if (DbTypeProp == null)
                DbTypeProp = _factory.CreateParameter()?.GetType().GetProperty("NpgsqlDbType")
                ?? throw new TypeInitializationException(nameof(PgDatabaseProvider), null);
        }

        public override bool NeedTypeConversion(Type typ)
        {
            return (typ == typeof(Json) || typ == typeof(Jsonb));
        }

        public static DbParameter MapParameter(DbParameter param, Jsonb value)
        {
            param.Value = (string)value;
            DbTypeProp.SetValue(param, Enum.Parse(DbTypeProp.PropertyType, "Jsonb"));
            return param;
        }

        public static DbParameter MapParameter(DbParameter param, Json value)
        {
            param.Value = (string)value;
            DbTypeProp.SetValue(param, Enum.Parse(DbTypeProp.PropertyType, "Json"));
            return param;
        }

    }
}