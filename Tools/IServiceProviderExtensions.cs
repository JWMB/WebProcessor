using Microsoft.Extensions.DependencyInjection;

public static class IServiceProviderExtensions
{
    public static T CreateInstance<T>(this IServiceProvider instance) where T : class
    {
        var constructors = typeof(T).GetConstructors();

        var constructor = constructors.First();
        var parameterInfo = constructor.GetParameters();

        var parameters = parameterInfo.Select(o => instance.GetRequiredService(o.ParameterType)).ToArray();

        return (T)constructor.Invoke(parameters);
    }
}
