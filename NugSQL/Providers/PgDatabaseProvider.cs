namespace NugSQL.Providers
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    public class PgDatabaseProvider : DatabaseProvider
    {
        private static PropertyInfo DbTypeProp;

        public PgDatabaseProvider()
        {
            var ass = "Npgsql.NpgsqlFactory, Npgsql";
            this._factory = GetFactory(ass);
            if(DbTypeProp == null) // TODO Static field should not depend on instance constructor!
                DbTypeProp = this._factory.CreateParameter().GetType().GetProperty("NpgsqlDbType");
        }

        public override bool NeedTypeConversion(Type typ)
        {
            return (typ == typeof(short) || typ == typeof(Json) || typ == typeof(Jsonb));
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

        public static DbParameter MappParameter(DbParameter param, short value)
        {
            param.Value = value;
            param.DbType = DbType.Int16;
            return param;
        }
    }
}