using Microsoft.Extensions.DependencyInjection;

namespace ProblemSource
{
    public static class IServiceProviderExtensions
    {
        public static T GetOrCreateInstance<T>(this IServiceProvider instance) where T : class
        {
            return instance.GetService<T>() ?? instance.CreateInstance<T>();
        }

        public static object GetOrCreateInstance(this IServiceProvider instance, Type type)
        {
            return instance.GetService(type) ?? instance.CreateInstance(type);
        }

        public static T CreateInstance<T>(this IServiceProvider instance) where T : class
        {
            return (T)instance.CreateInstance(typeof(T));
        }


        public static object CreateInstance(this IServiceProvider instance, Type type)
        {
            var constructors = type.GetConstructors();

            var constructor = constructors.First();
            var parameterInfo = constructor.GetParameters();

            var parameters = parameterInfo.Select(o => instance.GetRequiredService(o.ParameterType)).ToArray();

            return constructor.Invoke(parameters);
        }
    }
}
