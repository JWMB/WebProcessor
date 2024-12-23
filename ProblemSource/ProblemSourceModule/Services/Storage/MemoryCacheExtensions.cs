﻿using Microsoft.Extensions.Caching.Memory;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.Diagnostics;

namespace ProblemSource.Services.Storage
{
    public static class MemoryCacheExtensions
    {
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.

        //private static readonly Lazy<Func<MemoryCache, object>> GetCoherentState =
        //    new Lazy<Func<MemoryCache, object>>(() =>
        //        CreateGetter<MemoryCache, object>(
        //            typeof(MemoryCache)
        //                .GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance)));

        //private static readonly Lazy<Func<object, IDictionary>> GetEntries7 =
        //    new Lazy<Func<object, IDictionary>>(() =>
        //        CreateGetter<object, IDictionary>(
        //            typeof(MemoryCache)
        //                .GetNestedType("CoherentState", BindingFlags.NonPublic)
        //                .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)));

        #region Microsoft.Extensions.Caching.Memory_6_OR_OLDER

        private static readonly Lazy<Func<MemoryCache, object>> _getEntries6 =
            new(() => (Func<MemoryCache, object>)Delegate.CreateDelegate(
                typeof(Func<MemoryCache, object>),
                typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true),
                throwOnBindFailure: true));

        #endregion

        #region Microsoft.Extensions.Caching.Memory_7_OR_NEWER

        private static readonly Lazy<Func<MemoryCache, object>> _getCoherentState =
            new(() => CreateGetter<MemoryCache, object>(typeof(MemoryCache)
                .GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance)));

        #endregion

        #region Microsoft.Extensions.Caching.Memory_7_TO_8.0.8

        private static readonly Lazy<Func<object, IDictionary>> _getEntries7 =
            new(() => CreateGetter<object, IDictionary>(typeof(MemoryCache)
                .GetNestedType("CoherentState", BindingFlags.NonPublic)
                .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)));

        #endregion

        #region Microsoft.Extensions.Caching.Memory_8.0.10_OR_NEWER

        private static readonly Lazy<Func<object, IDictionary>> _getStringEntries8010 =
            new(() => CreateGetter<object, IDictionary>(typeof(MemoryCache)
                .GetNestedType("CoherentState", BindingFlags.NonPublic)
                .GetField("_stringEntries", BindingFlags.NonPublic | BindingFlags.Instance)));

        private static readonly Lazy<Func<object, IDictionary>> _getNonStringEntries8010 =
            new(() => CreateGetter<object, IDictionary>(typeof(MemoryCache)
                .GetNestedType("CoherentState", BindingFlags.NonPublic)
                .GetField("_nonStringEntries", BindingFlags.NonPublic | BindingFlags.Instance)));

        #endregion


        private static Func<TParam, TReturn> CreateGetter<TParam, TReturn>(FieldInfo field)
        {
            var methodName = $"{field.ReflectedType.FullName}.get_{field.Name}";
            var method = new DynamicMethod(methodName, typeof(TReturn), [typeof(TParam)], typeof(TParam), true);
            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, field);
            ilGen.Emit(OpCodes.Ret);
            return (Func<TParam, TReturn>)method.CreateDelegate(typeof(Func<TParam, TReturn>));
        }

        private static readonly Func<MemoryCache, IEnumerable> _getKeys =
            FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(MemoryCache)).Location) switch
            {
                { ProductMajorPart: < 7 } =>
                    static cache => ((IDictionary)_getEntries6.Value(cache)).Keys,
                { ProductMajorPart: < 8 } or { ProductMajorPart: 8, ProductMinorPart: 0, ProductBuildPart: < 10 } =>
                    static cache => _getEntries7.Value(_getCoherentState.Value(cache)).Keys,
                _ =>
                    static cache => ((ICollection<string>)_getStringEntries8010.Value(_getCoherentState.Value(cache)).Keys)
                        .Concat((ICollection<object>)_getNonStringEntries8010.Value(_getCoherentState.Value(cache)).Keys)
            };

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.

        public static IEnumerable GetKeys(this IMemoryCache memoryCache) =>
            _getKeys((MemoryCache)memoryCache);

        public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache) =>
            memoryCache.GetKeys().OfType<T>();
}

        //private static Func<TParam, TReturn> CreateGetter<TParam, TReturn>(FieldInfo field)
        //{
        //    if (field.ReflectedType == null)
        //        throw new NullReferenceException($"ReflectedType of {field.DeclaringType?.Name}.{field.Name}");
        //    var methodName = $"{field.ReflectedType.FullName}.get_{field.Name}";
        //    var method = new DynamicMethod(methodName, typeof(TReturn), new[] { typeof(TParam) }, typeof(TParam), true);
        //    var ilGen = method.GetILGenerator();
        //    ilGen.Emit(OpCodes.Ldarg_0);
        //    ilGen.Emit(OpCodes.Ldfld, field);
        //    ilGen.Emit(OpCodes.Ret);
        //    return (Func<TParam, TReturn>)method.CreateDelegate(typeof(Func<TParam, TReturn>));
        //}

        //private static readonly Func<MemoryCache, IDictionary> GetEntries;

        //static MemoryCacheExtensions() =>
        //    GetEntries = cache => GetEntries7.Value(GetCoherentState.Value(cache));

        //public static ICollection GetKeys(this IMemoryCache memoryCache) =>
        //    GetEntries((MemoryCache)memoryCache).Keys;

        //public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache) =>
        //    memoryCache.GetKeys().OfType<T>();
}
