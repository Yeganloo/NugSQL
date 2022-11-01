namespace NugSQL.Providers
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    public class MSSqlDatabaseProvider : DatabaseProvider
    {
        private static PropertyInfo DbTypeProp;
        private static readonly DbProviderFactory _factory;

        public override string ParameterPrefix { get; } = "@";

        public override DbProviderFactory Factory => _factory;

        static MSSqlDatabaseProvider()
        {
            _factory = GetFactory(
                nameof(MSSqlDatabaseProvider),
                "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient",
                "Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient");
            if (DbTypeProp == null)
                DbTypeProp = _factory.CreateParameter()?.GetType().GetProperty("SqlDbType")
                ?? throw new TypeInitializationException(nameof(MSSqlDatabaseProvider), null);
        }

        public static DbParameter MapParameter(DbParameter param, Json value)
        {
            param.Value = (string)value;
            DbTypeProp.SetValue(param, SqlDbType.NVarChar);
            return param;
        }

        public override bool NeedTypeConversion(Type typ)
        {
            return (typ == typeof(Json));
        }

    }
}