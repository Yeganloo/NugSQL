namespace NugSQL
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    using System.Reflection.Emit;
    using NugSQL.Providers;

    public abstract class Queries : IQueries, IDisposable
    {
        protected Func<IDataReader, Object>[] ResultGenerators
        {
            get
            {
                return (Func<IDataReader, Object>[])this.GetType()
                    .GetField("_ResultGenerators", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(null);
            }
        }
        protected DbProviderFactory _database;
        private string _connectionString;
        private DbConnection _sharedConnection;
        private int _sharedConnectionDepth;
        private IDbTransaction _transaction;



        public Queries(string cnn, DbProviderFactory factory)
        {
            this._database = factory;
            this._connectionString = cnn;
        }

        protected void OpenSharedConnection()
        {
            if (_sharedConnectionDepth++ == 0)
            {
                _sharedConnection = _database.CreateConnection() ?? throw new IOException(@$"Could not open a new Databse connection!
                Connection string: {this._connectionString}");
                _sharedConnection.ConnectionString = _connectionString;
            }
            if (_sharedConnection.State == ConnectionState.Broken)
                _sharedConnection.Close();
            if (_sharedConnection.State == ConnectionState.Closed)
                _sharedConnection.Open();
        }

        protected void CloseSharedConnection()
        {
            if (--_sharedConnectionDepth < 1)
            {
                _sharedConnection.Close();
            }
        }

        protected IDbCommand CreateCommand(IDbConnection cnn, IDbTransaction transaction)
        {
            var cmd = cnn.CreateCommand();
            cmd.Connection = cnn;
            cmd.CommandType = CommandType.Text;
            if (transaction != null)
            {
                try
                {
                    if (cnn == transaction.Connection)
                        cmd.Transaction = transaction;
                }
                catch
                {
                    _transaction = null;
                }
            }
            return cmd;
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            OpenSharedConnection();
            _transaction = _sharedConnection.BeginTransaction(isolationLevel);
            return _transaction;
        }

        protected int NonQuery(string query, DbParameter[] parameters)
        {
            try
            {
                OpenSharedConnection();
                using (var cmd = CreateCommand(_sharedConnection, this._transaction))
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                CloseSharedConnection();
            }
        }

        protected T Scalar<T>(string query, DbParameter[] parameters)
        {
            try
            {
                OpenSharedConnection();
                using (var cmd = CreateCommand(_sharedConnection, this._transaction))
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }
                    var res = cmd.ExecuteScalar();
                    if (res != null && !(res is T))
                        throw new InvalidCastException($"Query result type is: {res?.GetType().ToString() ?? "Null"} but {typeof(T)} expected!\r\nValue: {res}");
                    return (T)res;
                }
            }
            finally
            {
                CloseSharedConnection();
            }
        }

        protected T One<T>(string query, DbParameter[] parameters, int resGen)
        {
            try
            {
                OpenSharedConnection();
                using (var cmd = CreateCommand(_sharedConnection, this._transaction))
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ResultGenerators[resGen] != null ?
                                (T)ResultGenerators[resGen](reader)
                                : BuildResultGenerator<T>(reader, resGen);
                        }
                        else
                            throw new DataException("No Data");
                    }
                }
            }
            finally
            {
                CloseSharedConnection();
            }
        }

        protected IEnumerable<T> Query<T>(string query, DbParameter[] parameters, int resGen)
        {
            try
            {
                OpenSharedConnection();
                using (var cmd = CreateCommand(_sharedConnection, this._transaction))
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            yield return ResultGenerators[resGen] != null ?
                                (T)ResultGenerators[resGen](reader)
                                : BuildResultGenerator<T>(reader, resGen);
                        }
                        var gen = ResultGenerators[resGen];
                        while (reader.Read())
                        {
                            yield return (T)gen(reader);
                        }
                    }
                }
            }
            finally
            {
                CloseSharedConnection();
            }
        }

        private T BuildResultGenerator<T>(IDataReader reader, int index)
        {
            DatabaseProvider provider = (DatabaseProvider)this.GetType()
                .GetField("provider", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            var returnType = typeof(T);
            var recordType = typeof(IDataRecord);
            var isDbNull = recordType.GetMethod("IsDBNull");
            var gen = new DynamicMethod($"{returnType.Name}_generator", typeof(object), new Type[] { typeof(IDataReader) });
            var ilg = gen.GetILGenerator();


            if (returnType == typeof(Object))
            {
                // TODO Support Unknown Result Type
                // Should I User Expando Object or Anonymus Type?
            }
            else if (returnType.IsPrimitive)
            {
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Callvirt, recordType.GetMethod($"Get{reader.GetFieldType(0).Name}"));
            }
            else if (returnType == typeof(string))
            {
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Callvirt, recordType.GetMethod("GetString"));
            }
            else if (returnType == typeof(byte[]))
            {
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Callvirt, recordType.GetMethod($"GetValue"));
            }
            else
            {
                var rr = ilg.DeclareLocal(returnType);
                ilg.Emit(OpCodes.Newobj, returnType.GetConstructor(Type.EmptyTypes));
                ilg.Emit(OpCodes.Stloc, rr);
                //TODO add type check for every field to throw proper exception.
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var prop = returnType.GetProperty(reader.GetName(i));
                    if (prop == null)
                        continue;

                    var proptype = prop.PropertyType;

                    ilg.Emit(OpCodes.Ldarg_0); // Stack: /reader
                    ilg.Emit(OpCodes.Ldc_I4, i); // Stack: /reader/i
                    ilg.Emit(OpCodes.Callvirt, isDbNull); // Stack: /bool
                    var lblIsNull = ilg.DefineLabel();
                    ilg.Emit(OpCodes.Brtrue, lblIsNull);

                    // Check for converter
                    var converter = provider.GetType().GetMethod("Convert", new Type[] { reader.GetFieldType(i), proptype });


                    ilg.Emit(OpCodes.Ldloc, rr); // Stack: /result/
                    ilg.Emit(OpCodes.Ldarg_0); // Stack: /result/reader
                    ilg.Emit(OpCodes.Ldc_I4, i); // Stack: /result/reader/i

                    var getter = recordType.GetMethod($"Get{reader.GetFieldType(i).Name}", new Type[] { typeof(int) });
                    if (getter != null && converter == null)
                    {
                        ilg.Emit(OpCodes.Callvirt, getter); // Stack: /result/value
                        var mainType = Nullable.GetUnderlyingType(proptype);
                        if (mainType != null)
                        {
                            ilg.Emit(OpCodes.Newobj, proptype.GetConstructor(new Type[] { mainType }));
                        }
                    }
                    else
                    {
                        getter = recordType.GetMethod($"GetValue", new Type[] { typeof(int) });
                        ilg.Emit(OpCodes.Callvirt, getter); // Stack: /result/value
                        if (converter != null)
                        {
                            var defaultProp = ilg.DeclareLocal(proptype);
                            ilg.Emit(OpCodes.Ldloc, defaultProp);  // Stack: /result/value/DefaultValue
                            ilg.Emit(OpCodes.Call, converter); // Stack: /result/convertedValue
                        }
                    }
                    ilg.Emit(OpCodes.Callvirt, prop.SetMethod); // Stack: /
                    ilg.MarkLabel(lblIsNull);
                }
                ilg.Emit(OpCodes.Ldloc, rr);
            }
            ilg.Emit(OpCodes.Ret);
            var res = (Func<IDataReader, Object>)gen.CreateDelegate(typeof(Func<IDataReader, Object>));
            ResultGenerators[index] = res;
            return (T)res(reader);
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
            if (_sharedConnection.State != ConnectionState.Closed)
            {
                _sharedConnection.Close();
                _sharedConnectionDepth = 0;
            }
        }

    }
}