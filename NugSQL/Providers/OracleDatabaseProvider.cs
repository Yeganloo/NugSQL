namespace NugSQL.Providers
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    public class OracleDatabaseProvider : DatabaseProvider
    {
        private static PropertyInfo DbTypeProp;
        private static readonly DbProviderFactory _factory;

        public override string ParameterPrefix { get; } = ":";

        public override DbProviderFactory Factory => _factory;

        static OracleDatabaseProvider()
        {
            _factory = GetFactory(
                nameof(OracleDatabaseProvider),
                "Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess");
            if (DbTypeProp == null)
                DbTypeProp = _factory.CreateParameter()?.GetType().GetProperty("DbType")
                ?? throw new TypeInitializationException(nameof(OracleDatabaseProvider), null);
        }

        public override bool NeedTypeConversion(Type typ)
        {
            return false;
        }

    }
}