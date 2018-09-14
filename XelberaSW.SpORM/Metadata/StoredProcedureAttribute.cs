using System;
using System.Reflection;
using XelberaSW.SpORM.Internal;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class StoredProcedureAttribute : Attribute
    {

        private Type _parameterConverter;
        public string Name { get; }

        public StoredProcedureAttribute(string spName)
        {
            Name = spName;
        }

        public Type ParameterConverter
        {
            get => _parameterConverter;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!typeof(IParameterConverter).IsAssignableFrom(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"Type must implement {typeof(IParameterConverter).FullName}");
                }

                if (value.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"Type must have default consturtor available");
                }

                _parameterConverter = value;
            }
        }

        internal static IParameterConverter GetConverter(MethodInfo method)
        {
            var converter = method.GetCustomAttribute<StoredProcedureAttribute>()?._parameterConverter ?? typeof(DefaultParameterConverter);

            return (IParameterConverter)Activator.CreateInstance(converter);
        }

        internal static MethodInfo Convert { get; } = typeof(IParameterConverter).GetMethod(nameof(IParameterConverter.Convert));
    }
}
