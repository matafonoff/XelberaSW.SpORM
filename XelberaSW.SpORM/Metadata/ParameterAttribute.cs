using System;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ParameterAttribute : Attribute
    {
        public ParameterAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public Type Type { get; set; }
    }
}