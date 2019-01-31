/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace XelberaSW.SpORM.Utilities
{
    public static class Extensions
    {
        [DebuggerStepThrough]
        public static TValue GetOrAddValueSafe<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> func)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                lock (dict)
                {
                    if (!dict.TryGetValue(key, out value))
                    {
                        value = func(key);
                        dict.Add(key, value);
                    }
                }
            }

            return value;
        }

        public static bool IsAnonymousType(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        internal static Expression IsType<T>(this Expression expression)
        {
            return expression.IsType(typeof(T));
        }

        internal static Expression IsType(this Expression expression, Type type)
        {
            return Expression.TypeIs(expression, type);
        }


        internal static Expression IsNull(this Expression expression)
        {
            return Expression.ReferenceEqual(expression, Expression.Constant(null, expression.Type));
        }

        internal static BinaryExpression Assign(this ParameterExpression expr, object value)
        {
            if (value is Expression valueExpr)
            {
                return expr.Assign(valueExpr);
            }

            return Expression.Assign(expr, Expression.Constant(value));
        }

        internal static BinaryExpression Assign(this ParameterExpression expr, Expression value)
        {
            return Expression.Assign(expr, value);
        }

        internal static void AddCustomAttribute<TAttr, TArg>(this MethodBuilder mb, TArg argument)
        {
            var defaultCtor = typeof(TAttr).GetConstructor(new[] { typeof(TArg) });
            mb.SetCustomAttribute(new CustomAttributeBuilder(defaultCtor, new object[] { argument }));
        }

        internal static void AddCustomAttribute<TAttr, TArg1, TArg2>(this MethodBuilder mb, TArg1 arg1, TArg2 arg2)
        {
            var defaultCtor = typeof(TAttr).GetConstructor(new[] { typeof(TArg1), typeof(TArg2) });
            mb.SetCustomAttribute(new CustomAttributeBuilder(defaultCtor, new object[] { arg1, arg2 }));
        }

        internal static void AddCustomAttribute<TAttr>(this AssemblyBuilder mb)
            where TAttr : Attribute
        {
            var defaultCtor = typeof(TAttr).GetConstructor(Type.EmptyTypes);
            mb.SetCustomAttribute(new CustomAttributeBuilder(defaultCtor, new object[0]));
        }

        internal static void AddCustomAttribute<TAttr, TArg>(this AssemblyBuilder mb, TArg argument)
        {
            var defaultCtor = typeof(TAttr).GetConstructor(new[] { typeof(TArg) });
            mb.SetCustomAttribute(new CustomAttributeBuilder(defaultCtor, new object[] { argument }));
        }

        internal static void AddCustomAttribute<TAttr, TArg1, TArg2>(this AssemblyBuilder mb, TArg1 arg1, TArg2 arg2)
        {
            var defaultCtor = typeof(TAttr).GetConstructor(new[] { typeof(TArg1), typeof(TArg2) });
            mb.SetCustomAttribute(new CustomAttributeBuilder(defaultCtor, new object[] { arg1, arg2 }));
        }

        internal static void AddCustomAttribute<TAttr>(this MethodBuilder mb)
            where TAttr : Attribute
        {
            var defaultCtor = typeof(TAttr).GetConstructor(Type.EmptyTypes);
            mb.SetCustomAttribute(new CustomAttributeBuilder(defaultCtor, new object[0]));
        }

        public static byte[] ToBytes(this IntPtr pointer)
        {
            switch (IntPtr.Size)
            {
                case 4: return BitConverter.GetBytes(pointer.ToInt32());
                case 8: return BitConverter.GetBytes(pointer.ToInt64());
            }

            throw new InvalidOperationException();
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> enumerable, params T[] items)
        {
            return enumerable.Concat(items);
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> enumerable, params T[] items)
        {
            return items.Concat(enumerable);
        }
    }
}
