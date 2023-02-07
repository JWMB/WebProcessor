using System.Reflection.Emit;
using System.Reflection;
using System.Text;

namespace Common
{
    public static class DynamicTypeFactory
    {
        public static object CreateInstance(Dictionary<string, object> props)
        {
            var type = CreateType(props.ToDictionary(o => o.Key, o => o.Value.GetType()));
            var inst = CreateInstance(type);
            foreach (var item in props)
            {
                var prop = type.GetProperty(item.Key);
                if (prop == null) throw new NullReferenceException(item.Key);
                prop.SetValue(inst, item.Value);
            }
            return inst;
        }

        public static object CreateInstance(Type type, object values)
        {
            var instance = CreateInstance(type);
            var inputType = values.GetType();
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var val = inputType.GetProperty(prop.Name)!.GetValue(values);
                if (prop.PropertyType.Name.Contains("ReadOnlyMemory`") && val is string str)
                {
                    val = new ReadOnlyMemory<char>(Encoding.UTF8.GetBytes(str).Select(o => (char)o).ToArray());
                }
                prop.SetValue(instance, val);
            }

            return instance;
        }

        public static object CreateInstance(Type type)
        {
            var instance = Activator.CreateInstance(type);
            if (instance == null)
                throw new NullReferenceException($"Could not instantiate dynamic type {type.Name}");
            return instance;
        }

        public static Type CreateType(Dictionary<string, Type> props, AssemblyName? assemblyName = null)
        {
            assemblyName ??= new AssemblyName("DynamicInput");

            var dynamicClass = CreateTypeBuilder(assemblyName);
            CreateConstructor(dynamicClass);
            foreach (var item in props)
                CreateProperty(dynamicClass, item.Key, item.Value);
            return dynamicClass.CreateType();
        }

        public static Type CreateType(string[] propertyNames, Type[] Types, AssemblyName? assemblyName = null)
        {
            assemblyName ??= new AssemblyName("DynamicInput");
            if (propertyNames.Length != Types.Length)
                throw new ArgumentException("The number of property names should match their corresponding types number");

            var dynamicClass = CreateTypeBuilder(assemblyName);
            CreateConstructor(dynamicClass);
            for (int ind = 0; ind < propertyNames.Count(); ind++)
                CreateProperty(dynamicClass, propertyNames[ind], Types[ind]);
            return dynamicClass.CreateType();
        }

        private static TypeBuilder CreateTypeBuilder(AssemblyName assemblyName)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            var typeBuilder = moduleBuilder.DefineType(assemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return typeBuilder;
        }

        private static void CreateConstructor(TypeBuilder typeBuilder)
        {
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        }

        private static void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            var getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            var getIl = getPropMthdBldr.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);
            var setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });
            var setIl = setPropMthdBldr.GetILGenerator();
            var modifyProperty = setIl.DefineLabel();
            var exitSet = setIl.DefineLabel();
            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
