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
using System.Xml.Linq;
using System.Xml.XPath;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Internal.Readers
{
    class MetadataReader<T>
            where T : class, new()
    {
        private static readonly Lazy<Action<XElement, T>> _reader;
        private static readonly MethodInfo _getValueByXPath = typeof(MetadataReader<T>).GetMethod(nameof(GetValueByXPath), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo _trace = typeof(Trace).GetMethod(nameof(Trace.WriteLine), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(object), typeof(string) }, null);

        static MetadataReader()
        {
            _reader = new Lazy<Action<XElement, T>>(CreateReader);
        }

        public void Read(XElement source, T target) => GetReader()(source, target);
        public Action<XElement, T> GetReader() => _reader.Value;

        private static Action<XElement, T> CreateReader()
        {
            var source = Expression.Parameter(typeof(XElement), "src");
            var target = Expression.Parameter(typeof(T), "dst");
            var expressions = new List<Expression>();

            // Создать перенаправление значений из XML в модель

            Expression metadataRoot = target;

            var properties = typeof(T).GetProperties();

            var rootProperty = properties.FirstOrDefault(property => property.GetCustomAttribute<MetadataRootAttribute>() != null && !property.PropertyType.IsSimpleType());
            if (rootProperty != null)
            {
                metadataRoot = Expression.Property(target, rootProperty);

                if (rootProperty.CanWrite)
                {
                    expressions.Add(Expression.Assign(metadataRoot, Expression.New(rootProperty.PropertyType)));
                }

                properties = rootProperty.PropertyType.GetProperties();
            }

            foreach (var property in properties)
            {
                var path = property.GetCustomAttribute<MetadataXPathAttribute>()?.XPath;

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var exception = Expression.Variable(typeof(Exception), "ex");
                var assign = Expression.Assign(Expression.Property(metadataRoot, property), GetValueFromXml(expressions, source, path, property.PropertyType));

                var traceError = Expression.Call(_trace, exception, Expression.Constant($"Reading metadata property '{property.Name}' from '{path}' failed due to exception: "));
                expressions.Add(Expression.TryCatch(Expression.Block(assign, Expression.Empty()),
                                                    Expression.Catch(exception, Expression.Block(traceError, Expression.Empty()))));
            }

            var block = Expression.Block(expressions);
            var lambda = Expression.Lambda<Action<XElement, T>>(block, source, target);
            return lambda.Compile();
        }

        private static Expression GetValueFromXml(ICollection<Expression> expressions, ParameterExpression source, string xPath, Type targetType)
        {
            return ValueConverter.Default.GetConvertValueExpression(Expression.Call(_getValueByXPath, source, Expression.Constant(xPath)), targetType);
        }

        private static string GetValueByXPath(XElement root, string xPath)
        {
            var result = root.XPathEvaluate(xPath);

            if (result is IEnumerable enumerable)
            {
                var stringResult = enumerable.OfType<XAttribute>().Select(x => x.Value).FirstOrDefault();

                return stringResult;
            }

            throw new InvalidOperationException("Invalid result type got by XPathEvaluate");
        }
    }
}
