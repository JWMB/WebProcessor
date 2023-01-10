using Microsoft.Extensions.Caching.Memory;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models.Aggregates;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;

namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProvider
    {
        IBatchRepository<Phase> Phases { get; }
        IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        IBatchRepository<PhaseStatistics> PhaseStatistics { get; }

        IBatchRepository<TrainingSummary> TrainingSummaries { get; }
        IBatchRepository<UserGeneratedState> UserStates { get; }
    }

    public static class MemoryCacheExtensions
    {
        private static readonly Lazy<Func<MemoryCache, object>> GetCoherentState =
            new Lazy<Func<MemoryCache, object>>(() =>
                CreateGetter<MemoryCache, object>(typeof(MemoryCache)
                    .GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance)));

        private static readonly Lazy<Func<object, IDictionary>> GetEntries7 =
            new Lazy<Func<object, IDictionary>>(() =>
                CreateGetter<object, IDictionary>(typeof(MemoryCache)
                    .GetNestedType("CoherentState", BindingFlags.NonPublic)
                    .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)));

        private static Func<TParam, TReturn> CreateGetter<TParam, TReturn>(FieldInfo field)
        {
            var methodName = $"{field.ReflectedType.FullName}.get_{field.Name}";
            var method = new DynamicMethod(methodName, typeof(TReturn), new[] { typeof(TParam) }, typeof(TParam), true);
            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, field);
            ilGen.Emit(OpCodes.Ret);
            return (Func<TParam, TReturn>)method.CreateDelegate(typeof(Func<TParam, TReturn>));
        }

        private static readonly Func<MemoryCache, IDictionary> GetEntries;

        static MemoryCacheExtensions() =>
            GetEntries = cache => GetEntries7.Value(GetCoherentState.Value(cache));

        public static ICollection GetKeys(this IMemoryCache memoryCache) =>
            GetEntries((MemoryCache)memoryCache).Keys;

        public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache) =>
            memoryCache.GetKeys().OfType<T>();
    }
}
