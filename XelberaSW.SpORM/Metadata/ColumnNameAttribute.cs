using System;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ColumnNameAttribute : Attribute
    {
        public string Name { get; }

        public ColumnNameAttribute(string name)
        {
            Name = name;
        }
    }
}
