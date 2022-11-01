namespace NugSQL
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.IO;
    using System.Linq;
    using NugSQL.Providers;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class QuerBuilder
    {
        private static readonly Type baseType;
        private static readonly Type iFactory;
        private static readonly Type commandType;
        private static readonly Type dbFactory;
        private static readonly FieldInfo dbFieldRef;
        private static readonly MethodInfo CreateParamInfo;


        static QuerBuilder()
        {
            baseType = typeof(Queries);
            iFactory = typeof(DbProviderFactory);
            commandType = typeof(DbCommand);
            dbFactory = typeof(DbProviderFactory);
            dbFieldRef = baseType.GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            CreateParamInfo = dbFactory.GetMethod("CreateParameter", new Type[0]);
        }

        private static TypeBuilder GetBuilder(Type typ, DatabaseProvider provider)
        {
            // Create Type Builder
            var ass = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"nugsql_{Guid.NewGuid().ToString()}"),
                AssemblyBuilderAccess.Run);
            ModuleBuilder mod = ass.DefineDynamicModule("main");
            TypeBuilder tb = mod.DefineType($"NugSQL.impl.{typ.Name}",
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);

            // Inheritance
            tb.AddInterfaceImplementation(typ);
            tb.SetParent(baseType);
            var prv = tb.DefineField("provider", provider.GetType(), FieldAttributes.Private | FieldAttributes.Static);
            var gens = tb.DefineField("_ResultGenerators", typeof(Func<IDataReader, Object>[]),
                FieldAttributes.Private | FieldAttributes.Static);

            // Constructor
            var baseCtr = baseType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                null,
                new Type[] {typeof(string), dbFactory},
                null);
            var ctor = tb.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[]{typeof(string)});
            {
                var ilg = ctor.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0); // Stack: /this
                ilg.Emit(OpCodes.Ldarg_1); // Stack: /this/cnn
                ilg.Emit(OpCodes.Ldarg_0); // Stack: /this/cnn/this
                ilg.Emit(OpCodes.Ldfld, prv); // Stack: /this/cnn/provider
                ilg.Emit(OpCodes.Callvirt, provider.GetType().GetProperty("Factory").GetMethod); // this/cnn/factory
                ilg.Emit(OpCodes.Call, baseCtr); // Stack: /
                ilg.Emit(OpCodes.Ret);
            }

            return tb;
        }

        public static Type Compile<I>(string source, DatabaseProvider provider) where I : IQueries
        {
            return Compile<I>(ReadQueries(source), provider);
        }

        public static Type Compile<I>(Assembly assembly, DatabaseProvider provider) where I : IQueries
        {
            return Compile<I>(ReadQueries(assembly), provider);
        }

        public static Type Compile<I>(IEnumerable<RawQuery> queries, DatabaseProvider provider) where I : IQueries
        {
            var parameterType = provider.Factory.CreateParameter().GetType();
            var IType = typeof(I);
            var prvType = provider.GetType();
            var tb = GetBuilder(IType, provider);
            var resGenCount = 0;
            
            // Query Implementation
            
            foreach(var fn in IType.GetMethods())
            {
                RawQuery query = queries.SingleOrDefault(q => q.Name == fn.Name);
                var parameters = fn.GetParameters().ToArray();
                var mb = tb.DefineMethod(fn.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    fn.ReturnType,
                    parameters.Select(x => x.ParameterType).ToArray());
                var il = mb.GetILGenerator();

                if (query.Equals(default(RawQuery)))
                {
                    il.ThrowException(typeof(NotImplementedException));
                }
                else
                {
                    var arr = il.DeclareLocal(typeof(DbParameter[]));
                    // Create Parameters Array
                    il.Emit(OpCodes.Ldc_I4, parameters.Length);
                    il.Emit(OpCodes.Newarr, parameterType);
                    il.Emit(OpCodes.Stloc, arr);
                    // Stack: /
                    for(int i=0; i< parameters.Length; i++)
                    {
                        var parType = parameters[i].ParameterType;
                        il.Emit(OpCodes.Ldloc, arr);
                        // Stack: /arr/
                        if(parType.IsSubclassOf(parameterType))
                        {
                            il.Emit(OpCodes.Ldc_I4, i);
                            il.Emit(OpCodes.Ldarg, i + 1);
                            il.Emit(OpCodes.Stelem_Ref);
                            continue;
                        }
                        else
                        {
                            // Create New Parameter Object
                            il.Emit(OpCodes.Ldc_I4, i);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, dbFieldRef);
                            il.Emit(OpCodes.Callvirt, CreateParamInfo);
                            // Stack: /arr/i/p[i]/
                            // Set parameter name
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Ldstr, $"{provider.ParameterPrefix}{parameters[i].Name}");
                            il.Emit(OpCodes.Callvirt, parameterType.GetProperty(nameof(DbParameter.ParameterName)).SetMethod);
                            // Stack: /arr/i/p[i]/
                            var notNull = il.DefineLabel();
                            var isNull = il.DefineLabel();
                            if (!parType.IsValueType)
                            {
                                il.Emit(OpCodes.Ldarg, i + 1);
                                il.Emit(OpCodes.Ldnull);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Brfalse, notNull);
                                if (parameters[i].ParameterType.Name == "Byte[]")
                                {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Ldc_I4,(int)DbType.Binary);
                                    il.Emit(OpCodes.Callvirt, parameterType.GetProperty(nameof(DbParameter.DbType)).SetMethod);
                                }
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField("Value"));
                                il.Emit(OpCodes.Callvirt, parameterType.GetProperty(nameof(DbParameter.Value)).SetMethod);

                                il.Emit(OpCodes.Stelem_Ref);
                                // Stack: /
                                il.Emit(OpCodes.Br, isNull);
                            }
                            //else
                            il.MarkLabel(notNull);
                            // Stack: /arr/i/p[i]/
                            {
                                if(provider.NeedTypeConversion(parType))
                                {
                                    il.Emit(OpCodes.Ldarg, i + 1);
                                    il.Emit(OpCodes.Call,
                                        prvType.GetMethod("MappParameter",
                                            new Type[]{parameterType, parType}));
                                }
                                // TODO why this part fail if i set dbtype in IL?!
                                else if(Enum.TryParse<DbType>(parType.Name, true,out DbType dbtype))
                                {
                                    il.Emit(OpCodes.Ldarg, i + 1);
                                    if(parType.IsValueType)
                                    {
                                        il.Emit(OpCodes.Box, parType);
                                    }
                                    il.Emit(OpCodes.Ldc_I4, (int)dbtype);
                                    il.Emit(OpCodes.Call,
                                        typeof(DatabaseProvider).GetMethod("MappParameter",
                                            new Type[]{parameterType, typeof(object), typeof(DbType)}));
                                }
                                else
                                {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Ldarg, i + 1);
                                    il.Emit(OpCodes.Callvirt, parameterType.GetProperty(nameof(DbParameter.Value)).SetMethod);
                                }
                                il.Emit(OpCodes.Stelem_Ref);
                            }
                            il.MarkLabel(isNull);
                        }
                    }
                    // Stack: /
                    // Select query executor and pass the query + parameters
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, query.Query);
                    il.Emit(OpCodes.Ldloc, arr);
                    // Stack: this/query/arr/
                    if(fn.ReturnType == typeof(void))
                    {
                        il.Emit(OpCodes.Call,
                            baseType.GetMethod("NonQuery", BindingFlags.NonPublic | BindingFlags.Instance));
                        il.Emit(OpCodes.Pop); // Empty stack before return.
                    }
                    else
                    {
                        switch(query.ResultType)
                        {
                            default:
                            case ResultTypes.none:
                                il.Emit(OpCodes.Call,
                                    baseType.GetMethod("NonQuery", BindingFlags.NonPublic | BindingFlags.Instance));
                                il.Emit(OpCodes.Pop); // Empty stack before return.
                                break;
                            case ResultTypes.affected:
                                var mth = baseType.GetMethod("NonQuery", BindingFlags.NonPublic | BindingFlags.Instance);
                                il.Emit(OpCodes.Call, mth);
                                break;
                            case ResultTypes.scalar:
                                il.Emit(OpCodes.Call,
                                    baseType.GetMethod("Scalar", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .MakeGenericMethod(new Type[]{fn.ReturnType}));
                                break;
                            case ResultTypes.one:
                                il.Emit(OpCodes.Ldc_I4, resGenCount++);
                                il.Emit(OpCodes.Call,
                                    baseType.GetMethod("One", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .MakeGenericMethod(new Type[]{fn.ReturnType}));
                                break;
                            case ResultTypes.many:
                                il.Emit(OpCodes.Ldc_I4, resGenCount++);
                                il.Emit(OpCodes.Call,
                                    baseType.GetMethod("Query", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .MakeGenericMethod(new Type[]{fn.ReturnType.GenericTypeArguments[0]}));
                                break;
                        }
                    }
                    il.Emit(OpCodes.Ret);
                    tb.DefineMethodOverride(mb, fn);
                }
            }
            Type typ = tb.CreateType();
            typ.GetField("provider", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, provider);
            typ.GetField("_ResultGenerators", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, new Func<IDataReader, object>[resGenCount]);
            return typ;
        }

        public static IEnumerable<RawQuery> ReadQueries(string source)
        {
            List<RawQuery> res = new List<RawQuery>();
            foreach(var fl in Directory.GetFiles(source, "*.sql"))
            {
                ResultTypes resultType = ResultTypes.affected;
                string name = string.Empty;
                using(var f = new StreamReader(File.Open(fl, FileMode.Open, FileAccess.Read)))
                {
                    var cfg = f.ReadLine()?.Split(new char[]{':',' '}, StringSplitOptions.RemoveEmptyEntries);
                    for (var i=0; i < cfg?.Length; i++)
                    {
                        switch(cfg[i])
                        {
                            case "many":
                                resultType = ResultTypes.many;
                                break;
                            case "one":
                                resultType = ResultTypes.one;
                                break;
                            case "affected":
                                resultType = ResultTypes.affected;
                                break;
                            case "scalar":
                                resultType = ResultTypes.scalar;
                                break;
                            case "name":
                                name = cfg[++i];
                                break;
                        }
                    }
                    res.Add(new RawQuery(name, f.ReadToEnd(), resultType));
                }
            }
            return res;
        }

        public static IEnumerable<RawQuery> ReadQueries(Assembly source)
        {
            List<RawQuery> res = new List<RawQuery>();
            foreach(var fl in source.GetManifestResourceNames())
            {
                if(!fl.EndsWith(".sql"))
                    continue;
                ResultTypes resultType = ResultTypes.affected;
                string name = string.Empty;
                using(var f = new StreamReader(source.GetManifestResourceStream(fl)))
                {
                    var cfg = f.ReadLine()?.Split(new char[]{':',' '}, StringSplitOptions.RemoveEmptyEntries);
                    for (var i=0; i < cfg?.Length; i++)
                    {
                        switch(cfg[i])
                        {
                            case "many":
                                resultType = ResultTypes.many;
                                break;
                            case "one":
                                resultType = ResultTypes.one;
                                break;
                            case "affected":
                                resultType = ResultTypes.affected;
                                break;
                            case "scalar":
                                resultType = ResultTypes.scalar;
                                break;
                            case "name":
                                name = cfg[++i];
                                break;
                        }
                    }
                    res.Add(new RawQuery(name, f.ReadToEnd(), resultType));
                }
            }
            return res;
        }

        public static I New<I>(string connection, Type typ)
        {
            var res =  (I)Activator.CreateInstance(typ, connection);
            return res;
        }

    }
}
