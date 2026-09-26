using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Web
{
    public class TypedConfiguration
    {
        public static T Bind<T>(IConfigurationSection section) where T : new()
        {
            var instance = new T();
            section.Bind(instance);
            return instance;
        }

        public static T ConfigureTypedConfiguration<T>(IServiceCollection services, IConfiguration config, string sectionKey) where T : new()
        {
            // TODO: (low) continue investigation - how to avoid reflection and get validation errors immediately

            // Note: This does NOT cause validation on startup...: services.AddOptions<AceKnowledgeConfiguration>().Bind(config.GetSection("AceKnowledge")).ValidateDataAnnotations().ValidateOnStart();

            // https://referbruv.com/blog/posts/working-with-options-pattern-in-aspnet-core-the-complete-guide
            var appSettings = new T();

            //config.GetSection(sectionKey).Bind(appSettings);
            //services.AddSingleton(appSettings.GetType(), appSettings!);

            //RecurseBind(appSettings, services, config);
			XBind(appSettings, services, config.GetSection(typeof(T).Name));
			// https://kaylumah.nl/2021/11/29/validated-strongly-typed-ioptions.html
			// If we want to inject IOptions<Type> instead of just Type, this is needed: https://stackoverflow.com/a/61157181 services.ConfigureOptions(instance)
			//services.Configure<AceKnowledgeOptions>(config.GetSection("AceKnowledge"));

			return appSettings;
        }

		private static void XBind(object setting, IServiceCollection services, IConfiguration config)
		{
			config.Bind(setting);
			Rec(setting);
			void Rec(object s)
			{
				services.AddSingleton(s.GetType(), s);
				var props = s.GetType()
								.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				foreach (var item in props)
				{
					if (item.PropertyType == typeof(string) || item.PropertyType.IsPrimitive)
					{ }
					else if (item.PropertyType.IsAssignableTo(typeof(System.Collections.IEnumerable))
						&& item.PropertyType.IsGenericType)
					{ }
					else
					{
						var v = item.GetValue(s);
						if (v != null)
							Rec(v);
					}
				}
			}
		}

		private static void RecurseBind(object appSettings, IServiceCollection services, IConfiguration config)
        {
			var props = appSettings.GetType()
							.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
							; //.Where(o => !o.PropertyType.IsSealed); // TODO: (low) better check than IsSealed (also unit test)

			foreach (var prop in props)
			{
				var instance = prop.GetValue(appSettings);
				if (instance == null)
				{
					var isNullable = Nullable.GetUnderlyingType(prop.PropertyType) != null;
					if (isNullable)
						continue;
					if (prop.GetCustomAttributes(typeof(System.Runtime.CompilerServices.NullableAttribute), true).Any())
						continue;
					throw new Exception($"Null value for non-nullable property '{prop.Name}'");
				}
				//config.GetSection(prop.Name).Bind(instance);

				if (instance is System.Collections.IList lst)
				{
					foreach (var item in lst)
					{
						services.AddSingleton(item.GetType(), item);
						//RecurseBind(item, services, config);
					}
				}
				else
				{
					services.AddSingleton(instance!.GetType(), instance!);
					RecurseBind(instance, services, config);
				}

				//var asOptions = Microsoft.Extensions.Options.Options.Create(instance);
				//services.ConfigureOptions(instance);

				// Execute validation (if available)
				var validatorType = instance.GetType().Assembly.GetTypes()
					   .Where(t =>
					   {
						   var validatorInterface = t.GetInterfaces().SingleOrDefault(o =>
						   o.IsGenericType && o.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Options.IValidateOptions<>));
						   return validatorInterface != null && validatorInterface.GenericTypeArguments.Single() == instance.GetType();
					   }
					   ).FirstOrDefault();
				if (validatorType != null)
				{
					var validator = Activator.CreateInstance(validatorType);
					var m = validatorType.GetMethod("Validate");
					var result = (Microsoft.Extensions.Options.ValidateOptionsResult?)m?.Invoke(validator, new object[] { "", instance });
					if (result!.Failed)
					{
						throw new Exception($"{validatorType.Name}: {result.FailureMessage}");
					}
				}
			}
		}
    }
}
