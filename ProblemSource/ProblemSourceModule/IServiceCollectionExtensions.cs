using Microsoft.Extensions.DependencyInjection;

namespace ProblemSource
{
    public static class IServiceCollectionExtensions
    {
        public static void UpsertSingleton<TService>(this IServiceCollection services)
            where TService : class
            => services.Upsert<TService>(() => services.AddSingleton<TService>());

        public static void UpsertSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
            where TService : class
            =>
            services.Upsert<TService>(() => services.AddSingleton(sp => implementationFactory(sp)));

        public static void UpsertSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
            =>
            services.Upsert<TService, TImplementation>(() => services.AddSingleton<TService, TImplementation>());


        public static void Upsert<TService, TImplementation>(this IServiceCollection services, Action add)
            where TService : class
            where TImplementation : class, TService
        {
            services.RemoveService<TService>();
            services.RemoveService<TImplementation>();
            add();
        }
        public static void Upsert<TService>(this IServiceCollection services, Action add)
    where TService : class
        {
            services.RemoveService<TService>();
            add();
        }


        public static bool RemoveService<T>(this IServiceCollection services)
        {
            var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
                return true;
            }
            return false;
        }

    }
}
