namespace NugSQL.Providers
{
    using System;
    using System.Data.Common;

    public abstract class DatabaseProvider
    {
        protected DbProviderFactory _factory;

        public DbProviderFactory Factory
        {
            get
            {
                return _factory;
            }
        }

        public virtual bool NeedTypeConversion(Type typ)
        {
            return false;
        }
        
        protected DbProviderFactory GetFactory(params string[] assemblyQualifiedNames)
        {
            Type ft = null;
            foreach (var assemblyName in assemblyQualifiedNames)
            {
                ft = Type.GetType(assemblyName);
                if (ft != null)
                    break;
            }

            if (ft == null)
                throw new ArgumentException($"Could not load the {GetType().Name} DbProviderFactory.");

            return (DbProviderFactory) ft.GetField("Instance").GetValue(null);
        }
        
    }
}