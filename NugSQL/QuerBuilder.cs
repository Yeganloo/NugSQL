namespace NugSQL
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.IO;
    using System.Linq;
    using NugSQL.Providers;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class QuerBuilder
    {
        private static readonly Type basetype;
        private static readonly Type ifactory;
        private static readonly Type commandType;
        private static readonly Type dbFactory;
        private static readonly Type parameterType;
        private static readonly FieldInfo dbFieldRef;
        private static readonly MethodInfo CreateParamInfo;


        static QuerBuilder()
        {
            basetype = typeof(Queries);
            ifactory = typeof(DbProviderFactory);
            commandType = typeof(DbCommand);
            dbFactory = typeof(DbProviderFactory);
            parameterType = typeof(DbParameter);
            dbFieldRef = basetype.GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
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
            tb.SetParent(basetype);
            var prv = tb.DefineField("provider", provider.GetType(), FieldAttributes.Private | FieldAttributes.Static);
            var gens = tb.DefineField("_ResultGenerators", typeof(Func<IDataReader, Object>[]),
                FieldAttributes.Private | FieldAttributes.Static);

            // Constructor
            var baseCtr = basetype.GetConstructor(
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
            var IType = typeof(I);
            var prvType = provider.GetType();
            var tb = GetBuilder(IType, provider);
            var resGenCount = 0;
            
            // Query Implementation
            
            foreach(var fl in Directory.GetFiles(source, "*.sql"))
            {
                // TODO read the query
                ResultTypes resultType = ResultTypes.affected;
                string name = string.Empty;
                string query;
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
                    query = f.ReadToEnd();
                }
                MethodInfo fn = IType.GetMethod(name);
                if (fn == null)
                    continue;
                if(fn.ReturnType == typeof(void))
                    resultType = ResultTypes.none;
                var parameters = fn.GetParameters().ToArray();
                var mb = tb.DefineMethod(fn.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    fn.ReturnType,
                    parameters.Select(x => x.ParameterType).ToArray());
                {
                    var il = mb.GetILGenerator();
                    var arr = il.DeclareLocal(typeof(DbParameter[]));
                    // Create Parameters Array
                    il.Emit(OpCodes.Ldc_I4, parameters.Length);
                    il.Emit(OpCodes.Newarr, parameterType);
                    il.Emit(OpCodes.Stloc, arr);
                    // Stack: /
                    for(int i=0; i< parameters.Length; i++)
                    {
                        il.Emit(OpCodes.Ldloc, arr);
                        // Stack: /arr/
                        if(parameters[i].ParameterType.IsSubclassOf(parameterType))
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
                            //if (value == null)
                            il.Emit(OpCodes.Ldarg, i + 1);
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Ceq);
                            var notNull = il.DefineLabel();
                            var isNull = il.DefineLabel();
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
                            //else
                            il.MarkLabel(notNull);
                            // Stack: /arr/i/p[i]/
                            {
                                if(provider.NeedTypeConversion(parameters[i].ParameterType))
                                {
                                    il.Emit(OpCodes.Ldarg, i + 1);
                                    il.Emit(OpCodes.Call,
                                        prvType.GetMethod("MappParameter",
                                            new Type[]{parameterType, parameters[i].ParameterType}));
                                }
                                else
                                {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Ldarg, i + 1);
                                    il.Emit(OpCodes.Callvirt, parameterType.GetProperty(nameof(DbParameter.Value)).SetMethod);
                                    if(Enum.TryParse<DbType>(parameters[i].ParameterType.Name, true,out DbType dbtype))
                                    {
                                        Console.WriteLine("ok");
                                        il.Emit(OpCodes.Dup);
                                        il.Emit(OpCodes.Ldc_I4, (int)dbtype);
                                        il.Emit(OpCodes.Callvirt, parameterType.GetProperty(nameof(DbParameter.DbType)).SetMethod);
                                    }
                                }
                                il.Emit(OpCodes.Stelem_Ref);
                            }
                            il.MarkLabel(isNull);
                        }
                    }
                    // Stack: /
                    // Select query executor and pass the query + parameters
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, query);
                    il.Emit(OpCodes.Ldloc, arr);
                    // Stack: this/query/arr/
                    switch(resultType)
                    {
                        default:
                        case ResultTypes.none:
                            il.Emit(OpCodes.Call,
                                basetype.GetMethod("NonQuery", BindingFlags.NonPublic | BindingFlags.Instance));
                            il.Emit(OpCodes.Pop); // Empty stack before return.
                            break;
                        case ResultTypes.affected:
                            var mth = basetype.GetMethod("NonQuery", BindingFlags.NonPublic | BindingFlags.Instance);
                            il.Emit(OpCodes.Call, mth);
                            break;
                        case ResultTypes.scalar:
                            il.Emit(OpCodes.Call,
                                basetype.GetMethod("Scalar", BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(new Type[]{fn.ReturnType}));
                            break;
                        case ResultTypes.one:
                            il.Emit(OpCodes.Ldc_I4, resGenCount++);
                            il.Emit(OpCodes.Call,
                                basetype.GetMethod("One", BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(new Type[]{fn.ReturnType}));
                            break;
                        case ResultTypes.many:
                            il.Emit(OpCodes.Ldc_I4, resGenCount++);
                            il.Emit(OpCodes.Call,
                                basetype.GetMethod("Query", BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(new Type[]{fn.ReturnType.GenericTypeArguments[0]}));
                            break;
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

        public static I New<I>(string connection, Type typ)
        {
            var res =  (I)Activator.CreateInstance(typ, connection);
            return res;
        }

    }
}
