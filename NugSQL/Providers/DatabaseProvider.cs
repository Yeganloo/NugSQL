namespace NugSQL.Providers
{
    using System;
    using System.Data;
    using System.Data.Common;

    public abstract class DatabaseProvider
    {
        public abstract DbProviderFactory Factory { get; }

        public virtual bool NeedTypeConversion(Type typ)
        {
            return false;
        }

        public virtual string ParameterPrefix { get; } = ":";

        protected static DbProviderFactory GetFactory(string name, params string[] assemblyQualifiedNames)
        {
            Type? ft = null;
            foreach (var assemblyName in assemblyQualifiedNames)
            {
                ft = Type.GetType(assemblyName);
                if (ft != null)
                    break;
            }

            return (DbProviderFactory?)ft?.GetField("Instance")?.GetValue(null)
                ?? throw new ArgumentException($"Could not load the {name} DbProviderFactory.");
        }

        public static DbParameter MapParameter(DbParameter parameter, object obj, DbType dbType)
        {
            parameter.Value = obj;
            parameter.DbType = dbType;
            return parameter;
        }

    }
}