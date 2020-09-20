namespace NugSQL.Providers
{
    using System;
    using System.Data.Common;

    public abstract class DatabaseProvider
    {
        public abstract DbProviderFactory Factory{ get; }

        public virtual bool NeedTypeConversion(Type typ)
        {
            return false;
        }

        public virtual string ParameterPrefix { get; } = ":";
        
        protected static DbProviderFactory GetFactory(string name, params string[] assemblyQualifiedNames)
        {
            Type ft = null;
            foreach (var assemblyName in assemblyQualifiedNames)
            {
                ft = Type.GetType(assemblyName);
                if (ft != null)
                    break;
            }

            if (ft == null)
                throw new ArgumentException($"Could not load the {name} DbProviderFactory.");

            return (DbProviderFactory) ft.GetField("Instance").GetValue(null);
        }
        
    }
}