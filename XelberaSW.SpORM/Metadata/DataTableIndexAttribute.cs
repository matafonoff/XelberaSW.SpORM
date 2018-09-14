using System;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DataTableIndexAttribute : Attribute
    {
        public int Index { get; }

        public DataTableIndexAttribute(int index)
        {
            Index = index;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class IgnoreDataTableRangeAttribute : Attribute
    {
        public int Index { get; }
        public int Count { get; }

        public IgnoreDataTableRangeAttribute(int index, int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Value must be above 0");
            }

            Index = index;
            Count = count;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class IgnoreDataTableAttribute : IgnoreDataTableRangeAttribute
    {
        public IgnoreDataTableAttribute(int index)
        : base(index, 1)
        {
        }
    }
}
