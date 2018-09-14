using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace XelberaSW.SpORM.Internal
{
    static class TypeHelper
    {
        private static readonly List<Type> _primitiveTypes;

        private static readonly IReadOnlyList<Type> _integers = new []
        {
            typeof(sbyte), typeof(byte),
            typeof(ushort), typeof(short),
            typeof(uint), typeof(int),
            typeof(ulong), typeof(long),
        };

        private static readonly IReadOnlyList<Type> _floatPoints = new[]
        {
            typeof(float), typeof(double), typeof(decimal),
        };

        private static readonly IReadOnlyList<Type> _dateAndTime = new[]
        {
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan)
        };

        static TypeHelper()
        {
            
            var types = new List<Type>
            {
                typeof(string),
                typeof(Guid)
            };

            types.AddRange(_floatPoints);
            types.AddRange(_integers);
            types.AddRange(_dateAndTime);

            foreach (var type in types.ToArray())
            {
                if (type.IsValueType)
                {
                    types.Add(typeof(Nullable<>).MakeGenericType(type));
                }
            }

            _primitiveTypes = types;
        }

        public static Type GetRealType<T>()
        {
            return GetRealType(typeof(T));
        }

        public static bool IsSimpleType(this Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            if (_primitiveTypes.Contains(type))
            {
                return true;
            }

            return false;
        }

        public static bool IsDateTime(this Type type)
        {
            return _dateAndTime.Contains(type);
        }


        public static bool IsInteger(this Type type)
        {
            return _integers.Contains(type);
        }

        public static bool IsFloatPointNumber(this Type type)
        {
            return _floatPoints.Contains(type);
        }

        public static bool IsNumber(this Type type)
        {
            return IsInteger(type) || IsFloatPointNumber(type);
        }

        public static bool IsCollection<T>()
        {
            return IsCollection(typeof(T));
        }

        public static bool IsCollection(Type t)
        {
            return GetRealType(t) != t;
        }

        public static Type GetRealType(Type originalType)
        {
            if (originalType.IsArray)
            {
                return originalType.GetElementType();
            }

            var collection = IsCollectionInternal(originalType) ? originalType : originalType.GetInterfaces().FirstOrDefault(IsCollectionInternal);
            if (collection != null)
            {
                return collection.GetGenericArguments().Single();
            }

            return originalType;
        }

        private static bool IsCollectionInternal(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);
        }

        public static bool IsIntegerAssignable(Type sourceType, Type targetType)
        {
            if (!_integers.Contains(sourceType))
            {
                throw new ArgumentOutOfRangeException(nameof(sourceType), "Type must be one of integers");
            }

            if (!_integers.Contains(targetType))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Type must be one of integers");
            }

            return Marshal.SizeOf(sourceType) <= Marshal.SizeOf(targetType);
        }
    }
}

