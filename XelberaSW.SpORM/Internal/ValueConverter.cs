/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace XelberaSW.SpORM.Internal
{
    class ValueConverter
    {
        private static readonly Lazy<ValueConverter> _valueConverter = new Lazy<ValueConverter>(LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly MethodInfo _enumToObject = typeof(ValueConverter).GetMethod(nameof(GetEnumValue), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<PropertyInfo, Func<TypeConverter>> _typeConverters = new Dictionary<PropertyInfo, Func<TypeConverter>>();
        private static readonly MethodInfo _toString = typeof(object).GetMethod(nameof(ToString));

        private static readonly CultureInfo _enusCultureInfo = CultureInfo.GetCultureInfo("en-US");

        private static readonly ConstantExpression _numberFormatProvider = Expression.Constant(new DummyFormatProvider(_enusCultureInfo.NumberFormat), typeof(IFormatProvider));
        private static readonly ConstantExpression _dateTimeFormatProvider = Expression.Constant(new DummyFormatProvider(_enusCultureInfo.DateTimeFormat), typeof(IFormatProvider));
        private static readonly ConstantExpression _dummyFormatProvider = Expression.Constant(new DummyFormatProvider(null), typeof(IFormatProvider));

        private static readonly Dictionary<Type, MethodInfo> _converters = typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                                                          .Where(x => x.Name.StartsWith("To") &&
                                                                                                      ValidateMethodArgs(x, typeof(object)))
                                                                                          .ToDictionary(x => x.ReturnType);
        private static readonly Dictionary<Type, MethodInfo> _convertersWithFormatProvider = typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                                                                            .Where(x => x.Name.StartsWith("To") &&
                                                                                                                        ValidateMethodArgs(x, typeof(object), typeof(IFormatProvider)))
                                                                                                            .ToDictionary(x => x.ReturnType);
        private static readonly MethodInfo _changeType = typeof(Convert).GetMethod(nameof(Convert.ChangeType), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(object), typeof(Type) }, null);

        private class DummyFormatProvider : IFormatProvider
        {
            private readonly IFormatProvider _provider;

            public DummyFormatProvider(IFormatProvider provider)
            {
                _provider = provider;
            }

            public object GetFormat(Type formatType)
            {
                if (_provider != null)
                {

                }
                else
                {

                }
                return _provider?.GetFormat(formatType);
            }
        }

        private static bool ValidateMethodArgs(MethodInfo x, params Type[] paramTypes)
        {
            var parameters = x.GetParameters();

            if (parameters.Length != paramTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < paramTypes.Length; i++)
            {
                var methodParamType = parameters[i].ParameterType;
                var exprectedParamType = paramTypes[i];

                if (methodParamType != exprectedParamType)
                {
                    return false;
                }
            }

            return true;
        }

        private static TEnum GetEnumValue<TEnum>(object value)
            where TEnum : struct
        {
            if (value is TEnum enumValue)
            {
                return enumValue;
            }

            if (value is string stringValue)
            {
                return Enum.Parse<TEnum>(stringValue, true);
            }

            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }

        public static ValueConverter Default => _valueConverter.Value;

        public Expression GetConvertValueExpression(Expression expr, Type targetType)
        {
            var originalTargetType = targetType;
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            Expression resultExpression;

            var srcType = expr.Type;

            if (srcType == targetType)
            {
                resultExpression = expr;
            }
            else if (targetType.IsAssignableFrom(srcType))
            {
                resultExpression = Expression.Convert(expr, targetType);
            }
            else if (srcType.IsEnum && CheckSourceEnum(targetType, srcType))
            {
                resultExpression = Expression.Convert(expr, targetType);
            }
            else if (targetType.IsEnum)
            {
                resultExpression = Expression.Call(_enumToObject.MakeGenericMethod(targetType), expr);
            }
            else if (targetType == typeof(string))
            {
                resultExpression = Expression.Call(expr, _toString);
            }
            else if (typeof(IConvertible).IsAssignableFrom(srcType))
            {
                resultExpression = Expression.Convert(Expression.Call(_changeType, expr, Expression.Constant(targetType)), targetType);
            }
            else if (_converters.TryGetValue(targetType, out var converterMethodInfo))
            {
                var simpleConvert = Expression.Call(converterMethodInfo, expr);

                if (_convertersWithFormatProvider.TryGetValue(targetType, out var converterWithFormatProvider))
                {
                    var value = Expression.Variable(targetType);


                    Expression convertWithEnUsLocale;

                    var formatProvider = GetFormatProvider(targetType);
                    if (formatProvider != null)
                    {
                        convertWithEnUsLocale = Expression.Assign(value, Expression.Call(converterWithFormatProvider, expr, formatProvider));
                    }
                    else
                    {
                        convertWithEnUsLocale = Expression.Block(Expression.Throw(Expression.Constant(new InvalidCastException($"Could not convert value to {targetType.FullName}"))),
                            Expression.Default(targetType));
                    }

                    var tryCatch =
                        Expression.TryCatch(Expression.Assign(value, simpleConvert),
                            Expression.Catch(typeof(Exception), convertWithEnUsLocale));

                    resultExpression = Expression.Block(new[]
                    {
                        value
                    }, tryCatch, value);
                }
                else
                {
                    resultExpression = simpleConvert;
                }
            }
            else
            {
                Expression customConverterExpression;

                var convertedValue = Expression.Variable(targetType);

                var parser = targetType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[]
                {
                        typeof(string)
                    }, null);
                if (parser != null)
                {
                    customConverterExpression = Expression.Assign(convertedValue, Expression.Call(parser, Expression.Call(expr, _toString)));
                }
                else
                {
                    customConverterExpression = Expression.Throw(Expression.Constant(new InvalidCastException($"Value of type {srcType.FullName} could not be converted from type {targetType.FullName}")));
                }

                var expressions = new Expression[]
                {
                        Expression.IfThenElse(Expression.TypeIs(expr, targetType),
                                              Expression.Assign(convertedValue, Expression.Convert(expr, targetType)),
                                              customConverterExpression),
                        convertedValue
                };

                resultExpression = Expression.Block(new[]
                {
                        convertedValue
                    }, expressions);
            }

            if (originalTargetType != targetType)
            {
                resultExpression = Expression.Convert(resultExpression, originalTargetType);
            }



            return resultExpression;
        }

        private static ConstantExpression GetFormatProvider(Type targetType)
        {
            ConstantExpression formatProvider = null;
            if (targetType.IsDateTime())
            {
                formatProvider = _dateTimeFormatProvider;
            }
            else if (targetType.IsNumber())
            {
                formatProvider = _numberFormatProvider;
            }

            return formatProvider;
        }

        private static bool CheckSourceEnum(Type targetType, Type srcType)
        {
            var supportedType = srcType.GetEnumUnderlyingType();

            return targetType == supportedType || TypeHelper.IsIntegerAssignable(supportedType, targetType);
        }
    }
}
