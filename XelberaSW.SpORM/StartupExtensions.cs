using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using XelberaSW.SpORM.Utilities;

namespace XelberaSW.SpORM
{
    public static class StartupExtensions
    {
        public static IServiceCollection UseDbContext<T>(this IServiceCollection serviceCollection, Action<DbContextParameters> configure)
            where T : DbContext
        {
            serviceCollection.AddSingleton(ctx =>
            {
                var parameters = ctx.CreateInstance<DbContextParameters>();

                configure(parameters);

                return parameters;
            });

            serviceCollection.AddScoped<T>();
            serviceCollection.AddSingleton<CustomXmlSerializer>();

            return serviceCollection;
        }
    }

    public static class ServiceProviderExtensions
    {
        private static readonly Dictionary<Type, Func<IServiceProvider, object>> _builders = new Dictionary<Type, Func<IServiceProvider, object>>();

        private static readonly MethodInfo _getService = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService));

        public static T CreateInstance<T>(this IServiceProvider serviceProvider)
            where T : class
        {
            return (T)_builders.GetOrAddValueSafe(typeof(T), t => CreateInstanceBulider(t, serviceProvider))?.Invoke(serviceProvider);
        }

        private static Func<IServiceProvider, object> CreateInstanceBulider(Type type, IServiceProvider serviceProvider)
        {
            var providerParameter = Expression.Parameter(typeof(IServiceProvider), "provider");

            var defaultCtor = type.GetConstructor(Type.EmptyTypes);

            Expression<Func<IServiceProvider, object>> defaultNewExpression = null;
            if (defaultCtor != null)
            {
                defaultNewExpression = Expression.Lambda<Func<IServiceProvider, object>>(Expression.Convert(Expression.New(defaultCtor), typeof(object)), providerParameter);
            }
            else
            {
                var arguments = new List<Expression>();

                foreach (var info in type.GetConstructors()
                                         .Select(x => new
                                         {
                                             Ctor = x,
                                             Parameters = x.GetParameters()
                                         })
                                         .OrderBy(x => x.Parameters.Length))
                {
                    arguments.Clear();

                    var createInstance = true;

                    foreach (var parameter in info.Parameters)
                    {
                        if (parameter.IsRetval ||
                            parameter.IsOut)
                        {
                            createInstance = false;
                            break;
                        }

                        var paramValue = serviceProvider.GetService(parameter.ParameterType);
                        if (paramValue == null)
                        {
                            if (parameter.HasDefaultValue)
                            {
                                arguments.Add(Expression.Constant(parameter.DefaultValue, parameter.ParameterType));
                            }
                            else
                            {
                                createInstance = false;
                                break;
                            }
                        }
                        else
                        {
                            arguments.Add(Expression.Convert(Expression.Call(providerParameter, _getService, Expression.Constant(parameter.ParameterType)), parameter.ParameterType));
                        }
                    }

                    if (createInstance)
                    {
                        defaultNewExpression = Expression.Lambda<Func<IServiceProvider, object>>(Expression.Convert(Expression.New(info.Ctor, arguments), typeof(object)), providerParameter);
                        break;
                    }
                }
            }

            return defaultNewExpression?.Compile();
        }
    }

}
