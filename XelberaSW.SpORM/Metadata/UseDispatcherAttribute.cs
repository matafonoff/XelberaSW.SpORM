using System;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UseDispatcherAttribute : Attribute
    {
        public UseDispatcherAttribute(string dispatcherName = null)
        {
            Dispatcher = dispatcherName;
        }

        public UseDispatcherAttribute() : this(null)
        { }

        public string Dispatcher { get; }

        public MethodInfo GetDispatcherMethod(Type t)
        {
            var disp = Dispatcher;

            if (string.IsNullOrWhiteSpace(disp))
            {
                disp = "Dispatcher";
            }

            var mi = t.GetMethod(disp, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi == null)
            {
                if (t.GetCustomAttribute<UseDispatcherAttribute>() == null)
                {
                    return null;
                }

                throw new MissingMethodException(t.FullName, disp);
            }

            if (!ValidateMethod(mi))
            {
                throw new MissingMethodException($"Method {disp} must have following signature: {nameof(Task<IDataReader>)} {disp}(string actionName, object parameters);");
            }

            return mi;
        }

        private bool ValidateMethod(MethodInfo methodInfo)
        {
            return ValidateReturnValue(methodInfo) &&
                   ValidateParameters(methodInfo);
        }

        private bool ValidateParameters(MethodInfo mi)
        {
            var parameters = mi.GetParameters();

            if (parameters.Length == 2)
            {
                return parameters[0].ParameterType == typeof(string) &&
                       !parameters[1].ParameterType.IsValueType;
            }

            if (parameters.Length == 3)
            {
                return parameters[0].ParameterType == typeof(string) &&
                       !parameters[1].ParameterType.IsValueType &&
                        parameters[2].ParameterType == typeof(CancellationToken);
            }

            return false;
        }

        private static bool ValidateReturnValue(MethodInfo methodInfo)
        {
            return typeof(Task<IDataReader>).IsAssignableFrom(methodInfo.ReturnType);
        }
    }
}