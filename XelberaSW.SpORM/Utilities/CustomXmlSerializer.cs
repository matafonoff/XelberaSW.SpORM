/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Utilities
{
    public class CustomXmlSerializer
    {
        private static readonly Type[] _writeTypes =
        {
            typeof(string),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(DateTimeOffset),
            typeof(Enum),
            typeof(decimal),
            typeof(Guid)
        };

        internal static CustomXmlSerializer Instance { get; } = new CustomXmlSerializer();

        private static readonly Dictionary<Type, Func<object, string, XElement>> _serializers = new Dictionary<Type, Func<object, string, XElement>>();
        private static readonly MethodInfo _stringIsNullOrEmptyMethodInfo = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), BindingFlags.Static | BindingFlags.Public);
        private static readonly MethodInfo _encodeName = typeof(XmlConvert).GetMethod(nameof(XmlConvert.EncodeName), BindingFlags.Static | BindingFlags.Public);
        private static readonly MethodInfo _setAttributeValue = typeof(XElement).GetMethod(nameof(XElement.SetAttributeValue));
        private static readonly MethodInfo _setElementValue = typeof(XElement).GetMethod(nameof(XElement.SetElementValue));
        private static readonly MethodInfo _setValue = typeof(XElement).GetMethod(nameof(XElement.SetValue));
        private static readonly MethodInfo _add = typeof(XElement).GetMethod(nameof(XElement.Add), new[] { typeof(object) });
        private static readonly MethodInfo _dispose = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose));
        private static readonly MethodInfo _getEnumerator = typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator));
        private static readonly MethodInfo _moveNext = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext));
        private static readonly MethodInfo _getCurrent = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current)).GetMethod;
        private static readonly MethodInfo _serialize = typeof(CustomXmlSerializer).GetMethod(nameof(Serialize));

        public XElement Serialize(object obj, string rootName)
        {
            if (obj == null)
            {
                return null;
            }

            var serializer = GetSerializer(obj.GetType(), rootName);

            return serializer(obj, rootName);
        }

        [DebuggerStepThrough]
        private Func<object, string, XElement> GetSerializer(Type t, string rootName)
        {
            return _serializers.GetOrAddValueSafe(t, type => CreateSerializer(t, rootName));
        }

        [DebuggerStepThrough]
        private static Expression AsXName(Expression expr)
        {
            return Expression.Convert(expr, typeof(XName));
        }

        [DebuggerStepThrough]
        private static Expression AsXName(object expr)
        {
            return AsXName(Expression.Constant(expr));
        }

        private Func<object, string, XElement> CreateSerializer(Type type, string rootName)
        {
            if (string.IsNullOrEmpty(rootName))
            {
                rootName = type.GetCustomAttribute<CompilerGeneratedAttribute>() != null ? "Object" : type.Name;
            }

            var root = type.GetCustomAttribute<XmlRootAttribute>();
            if (root != null)
            {
                var rootNameFromMetadata = new StringBuilder();

                if (!string.IsNullOrEmpty(root.Namespace))
                {
                    rootNameFromMetadata.Append(root.Namespace).Append(":");
                }

                rootNameFromMetadata.Append(!string.IsNullOrEmpty(root.ElementName) ? root.ElementName : rootName);

                rootName = rootNameFromMetadata.ToString();
            }
            rootName = XmlConvert.EncodeName(rootName);

            // Parameters of Lambda Expression
            var rootNameParameter = Expression.Parameter(typeof(string), nameof(rootName));
            var objectParameter = Expression.Parameter(typeof(object));

            var expressions = new List<Expression>();
            var endOfMethod = Expression.Label();

            // Local valiables
            var retVal = Expression.Variable(typeof(XElement), "elem");
            var objectVariable = Expression.Variable(type, "obj");

            // obj = (T)$objectParameter
            expressions.Add(Expression.Assign(objectVariable, Expression.Convert(objectParameter, type)));

            // if (string.IsNullOrEmpty(rootName)) rootName = %rootName%;
            var checkedIfNoRootNamespace = Expression.Call(_stringIsNullOrEmptyMethodInfo, rootNameParameter);
            expressions.Add(Expression.IfThenElse(checkedIfNoRootNamespace,
                                                  Expression.Assign(rootNameParameter, Expression.Constant(rootName)),
                                                  Expression.Assign(rootNameParameter, Expression.Call(_encodeName, rootNameParameter))));

            // elem = new XElement((XName)rootName);
            var createXElement = Expression.New(typeof(XElement).GetConstructor(new[] { typeof(XName) }), AsXName(rootNameParameter));
            expressions.Add(Expression.Assign(retVal, createXElement));

            var variables = new List<ParameterExpression>
            {
                retVal, objectVariable
            };

            if (type.IsPrimitive || type == typeof(string))
            {
                expressions.Add(Expression.Call(retVal, _setValue, objectParameter));
            }
            else
            {
                foreach (var property in type.GetProperties())
                {
                    if (property.GetCustomAttribute<XmlIgnoreAttribute>() != null)
                    {
                        continue;
                    }

                    if (CanBeUsedAsAttributeValue(property))
                    {
                        AddAttribute(property, expressions, retVal, objectVariable);
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) &&
                             property.PropertyType != typeof(string))
                    {
                        // arrays
                        AddArray(property, expressions, retVal, objectVariable, variables);
                    }
                    else
                    {
                        AddElement(property, expressions, retVal, objectVariable);
                    }
                }
            }

            // return elem;
            expressions.Add(Expression.Return(endOfMethod, retVal));
            expressions.Add(Expression.Label(endOfMethod));
            expressions.Add(retVal);

            var expression = Expression.Block(variables.Distinct(), expressions);
            var method = Expression.Lambda<Func<object, string, XElement>>(expression, objectParameter, rootNameParameter);

            return method.Compile();
        }

        private void AddArray(PropertyInfo property, List<Expression> outerExpressions, ParameterExpression retVal, ParameterExpression objectVariable, List<ParameterExpression> variables)
        {
            var name = GetNameFromXmlElementAttribute(property, out var hasAttribute);

            var enumerableVariable = Expression.Variable(typeof(IEnumerator));

            var expressions = new List<Expression>();

            if (!hasAttribute)
            {
                var rootName = name;
                var arrayRootVariable = Expression.Variable(typeof(XElement));

                expressions.Add(Expression.Assign(arrayRootVariable, Expression.New(typeof(XElement).GetConstructor(new[] { typeof(XName) }), AsXName(rootName))));
                expressions.Add(Expression.Call(retVal, _add, arrayRootVariable));

                retVal = arrayRootVariable;

                variables.Add(arrayRootVariable);
            }

            if (property.PropertyType.IsArray)
            {
                if (property.PropertyType.GetElementType() == typeof(object))
                {
                    name = "anyType";
                }
            }

            var enumerable = GetPropValue(objectVariable, property, false);
            expressions.Add(Expression.Assign(enumerableVariable, Expression.Call(enumerable, _getEnumerator)));



            var endOfLoop = Expression.Label();

            var getXElement = Expression.Call(Expression.Constant(Instance), _serialize, Expression.Property(enumerableVariable, _getCurrent), Expression.Constant(name));

            var loop = Expression.IfThenElse(Expression.IsTrue(Expression.Call(enumerableVariable, _moveNext)),
                                             Expression.Call(retVal, _add, getXElement),
                                             Expression.Break(endOfLoop));


            expressions.Add(Expression.Loop(loop));

            expressions.Add(Expression.Label(endOfLoop));

            var disposable = Expression.Variable(typeof(IDisposable));
            expressions.Add(Expression.Assign(disposable, Expression.TypeAs(enumerableVariable, typeof(IDisposable))));

            var localVariables = new[]
            {
                enumerableVariable,
                disposable
            };

            var expression = Expression.TryFinally(Expression.Block(localVariables, expressions),
                                                   Expression.IfThen(Expression.ReferenceNotEqual(disposable, Expression.Constant(null, disposable.Type)),
                                                                     Expression.Call(disposable, _dispose)));

            variables.AddRange(localVariables);

            outerExpressions.Add(expression);
        }

        private void AddElement(PropertyInfo property, List<Expression> expressions, ParameterExpression retVal, ParameterExpression objectVariable)
        {
            var name = GetNameFromXmlElementAttribute(property);

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            Expression resultExpression;

            var isNullable = type != property.PropertyType;

            var valueOfNullableProperty =
                isNullable ? property.PropertyType.GetProperty(nameof(Nullable<int>.Value)) : null;
            var hasValueProperty =
                isNullable ? property.PropertyType.GetProperty(nameof(Nullable<int>.HasValue)) : null;

            Expression GetProperty(bool convertToObject)
            {
                if (isNullable)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    Expression propValue = Expression.Property(GetPropValue(objectVariable, property, false),
                        valueOfNullableProperty);

                    if (convertToObject)
                    {
                        propValue = Expression.Convert(propValue, typeof(object));
                    }

                    return propValue;
                }

                return GetPropValue(objectVariable, property, convertToObject);
            }


            var converterMethod = GetConverterMethod(property);
            if (converterMethod != null)
            {
                var call = converterMethod.IsStatic
                    ? Expression.Call(converterMethod, GetProperty(false))
                    : Expression.Call(objectVariable, converterMethod, GetProperty(false));

                resultExpression = Expression.Call(retVal, _setElementValue, AsXName(name), call);
            }
            else
            {
                if (type.IsPrimitive ||
                    type == typeof(string))
                {
                    resultExpression = Expression.Call(retVal, _setElementValue, AsXName(name), GetProperty(true));
                }
                else if (type.IsEnum)
                {
                    resultExpression = Expression.Call(retVal, _setElementValue, AsXName(name),
                        Expression.Convert(GetProperty(false), Enum.GetUnderlyingType(type)));
                }
                else if (type == typeof(TimeSpan))
                {
                    var formatter = type.GetMethod(nameof(ToString), BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[] { typeof(string) }, null);

                    // <retval>.SetAttributeValue(<name>, <property>.ToString("g"))
                    resultExpression = Expression.Call(retVal, _setElementValue, AsXName(name),
                        Expression.Call(GetProperty(false), formatter, Expression.Constant("g")));
                }
                else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                {
                    var formatter = type.GetMethod(nameof(ToString), BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[] { typeof(string) }, null);

                    // <retval>.SetAttributeValue(<name>, <property>.ToString("O"))
                    resultExpression = Expression.Call(retVal, _setElementValue, AsXName(name),
                        Expression.Call(GetProperty(false), formatter, Expression.Constant("O")));
                }
                else
                {
                    Expression<Func<object, string, XElement>> bind = (o, s) => GetSerializer(o.GetType(), s)(o, s);

                    var createChildTree = Expression.Invoke(bind, GetProperty(true), Expression.Constant(name));
                    resultExpression = Expression.Call(retVal, _add, createChildTree);
                }
               

                if (isNullable)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    resultExpression = Expression.IfThen(
                        Expression.Property(GetPropValue(objectVariable, property, false), hasValueProperty),
                        resultExpression);
                }
                else if (!type.IsValueType)
                {
                    resultExpression = Expression.IfThen(
                        Expression.NotEqual(GetPropValue(objectVariable, property, false), Expression.Constant(null, type)),
                        resultExpression);
                }
            }

            expressions.Add(resultExpression);
        }



        private void AddAttribute(PropertyInfo property, List<Expression> expressions, ParameterExpression retVal, ParameterExpression objectVariable)
        {
            var name = GetNameFromXmlElementAttribute(property);

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var isNullable = type != property.PropertyType;

            var valueOfNullableProperty = isNullable ? property.PropertyType.GetProperty(nameof(Nullable<int>.Value)) : null;
            var hasValueProperty = isNullable ? property.PropertyType.GetProperty(nameof(Nullable<int>.HasValue)) : null;

            Expression resultExpression;

            var converterMethod = GetConverterMethod(property);
            if (converterMethod != null)
            {
                var propValue = GetPropValue(objectVariable, property, false);
                var call = converterMethod.IsStatic
                    ? Expression.Call(converterMethod, propValue)
                    : Expression.Call(objectVariable, converterMethod, propValue);

                resultExpression = Expression.Call(retVal, _setAttributeValue, AsXName(name), call);
            }
            else
            {
                var getPropValue = isNullable
                    ? Expression.Convert(
                        Expression.Property(GetPropValue(objectVariable, property, false), valueOfNullableProperty),
                        typeof(object))
                    : GetPropValue(objectVariable, property);

                resultExpression = Expression.Call(retVal, _setAttributeValue, AsXName(name), getPropValue);

                if (isNullable)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    resultExpression = Expression.IfThen(
                        Expression.Property(GetPropValue(objectVariable, property, false), hasValueProperty),
                        resultExpression);
                }
            }

            expressions.Add(resultExpression);
        }

        private static MethodInfo GetConverterMethod(PropertyInfo property)
        {
            var converterMethod = property.DeclaringType.GetMethod($"GetValueOf_{property.Name}",
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { property.PropertyType }, null);
            return (converterMethod != null &&
                    converterMethod.ReturnType == typeof(object) ? converterMethod : null);
        }

        private string GetNameFromXmlElementAttribute(PropertyInfo property)
        {
            return GetNameFromXmlElementAttribute(property, out var _);
        }

        private string GetNameFromXmlElementAttribute(PropertyInfo property, out bool hasAttribute)
        {
            hasAttribute = false;
            var name = property.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? property.Name;

            var meta = property.GetCustomAttribute<XmlElementAttribute>();
            if (meta != null)
            {
                hasAttribute = true;

                var fromMetadata = new StringBuilder();

                if (!string.IsNullOrEmpty(meta.Namespace))
                {
                    fromMetadata.Append(meta.Namespace).Append(":");
                }

                fromMetadata.Append(!string.IsNullOrEmpty(meta.ElementName) ? meta.ElementName : name);

                name = fromMetadata.ToString();
            }

            name = XmlConvert.EncodeName(name);
            return name;
        }

        private Expression GetPropValue(ParameterExpression objectVariable, PropertyInfo property, bool cast = true)
        {
            Expression expr = Expression.Property(objectVariable, property);

            return cast ? Expression.Convert(expr, typeof(object)) : expr;

        }

        private bool CanBeUsedAsAttributeValue(PropertyInfo property)
        {
            if (property.GetCustomAttribute<XmlAttributeAttribute>() != null)
            {
                return true;
            }

            if (property.GetCustomAttribute<XmlElementAttribute>() != null)
            {
                return false;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            return type.IsPrimitive || _writeTypes.Contains(type) || type.IsEnum;
        }

    }
}

