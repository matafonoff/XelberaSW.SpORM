/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using XelberaSW.SpORM.Metadata;
using XelberaSW.SpORM.Utilities;

// ReSharper disable StaticMemberInGenericType

namespace XelberaSW.SpORM.Internal.Readers
{
    class SingleModelReader<T> : ModelReaderBase<IDataRecord, T>, ISingleEntityMetadataProvider
    {
        private static readonly MethodInfo _convert = typeof(TypeConverter).GetMethod(nameof(TypeConverter.ConvertFrom), new[] { typeof(object) });
        private static readonly Dictionary<PropertyInfo, Func<TypeConverter>> _typeConverters = new Dictionary<PropertyInfo, Func<TypeConverter>>();
        private static readonly PropertyInfo _indexer = typeof(IDataRecord).GetProperty("Item", typeof(object), new[] { typeof(string) });
        private static readonly PropertyInfo _stringLength = typeof(string).GetProperty(nameof(string.Length));
        private static readonly Dictionary<string, List<PropertyInfo>> _staticProperties = new Dictionary<string, List<PropertyInfo>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Type _iBeforeReadEntity = typeof(IBeforeReadEntity);
        private static readonly MethodInfo _preprocessEntity = _iBeforeReadEntity.GetMethod(nameof(IBeforeReadEntity.PreProcessEntity));

        private static readonly Type _iAfterReadEntity = typeof(IAfterReadEntity);
        private static readonly MethodInfo _postprocessEntity = _iAfterReadEntity.GetMethod(nameof(IAfterReadEntity.PostProcessEntity));


        private readonly Dictionary<string, List<PropertyInfo>> _properties;

        private readonly ValueConverter _valueConverter = new ValueConverter();

        static SingleModelReader()
        {
            InitializeProperties(typeof(T).GetProperties(), _staticProperties);
        }

        private static void InitializeProperties(IEnumerable<PropertyInfo> propertyInfos, Dictionary<string, List<PropertyInfo>> properties)
        {
            foreach (var prop in propertyInfos)
            {
                if (prop.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                {
                    continue;
                }

                if (!prop.PropertyType.IsSimpleType())
                {
                    Trace.WriteLine($"Property {prop.DeclaringType.FullName}::{prop.Name} has unsupported type: {prop.PropertyType.FullName}", "SpORM");
                    continue;
                }

                if (!prop.CanWrite)
                {
                    Trace.WriteLine($"Property {prop.DeclaringType.FullName}::{prop.Name} is readonly", "SpORM");
                    continue;
                }

                var name = prop.GetCustomAttribute<ColumnNameAttribute>()?.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = prop.Name;
                }

                var key = name.ToLower();
                if (!properties.TryGetValue(key, out var props))
                {
                    properties[key] = props = new List<PropertyInfo>();
                }

                props.Add(prop);
            }
        }

        public SingleModelReader()
        {
            _properties = _staticProperties;
        }

        public SingleModelReader(IEnumerable<PropertyInfo> propertyInfos)
        {
            _properties = new Dictionary<string, List<PropertyInfo>>();
            InitializeProperties(propertyInfos, _properties);
        }

        /// <inheritdoc />
        protected override Func<IDataRecord, T> CreateReader()
        {
            var dataRecordParameter = Expression.Parameter(typeof(IDataRecord), "dataRecord");
            var objVariable = Expression.Variable(typeof(object), "value");
            var entity = Expression.Variable(typeof(T), "entity");

            var expressions = new List<Expression>
            {
                entity.Assign(Expression.New(entity.Type))
            };

            var preprocessEntityMethod = _iBeforeReadEntity.IsAssignableFrom(entity.Type) ? _preprocessEntity : entity.Type.GetMethod(nameof(IBeforeReadEntity.PreProcessEntity), BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (preprocessEntityMethod != null)
            {
                expressions.Add(Expression.Call(entity, preprocessEntityMethod, dataRecordParameter));
            }

            var customReader = CustomReaderAttribute.GetReader<T>();
            if (customReader != null)
            {
                expressions.Add(Expression.Call(Expression.Constant(customReader), CustomReaderAttribute.ReadMethod, Expression.Convert(entity, typeof(object)), dataRecordParameter));
            }
            else
            {
                foreach (var property in _properties)
                {
                    expressions.Add(objVariable.Assign(Expression.MakeIndex(dataRecordParameter, _indexer, new Expression[] { Expression.Constant(property.Key) })));

                    foreach (var prop in property.Value)
                    {
                        expressions.Add(Expression.IfThenElse(Expression.Or(Expression.TypeIs(objVariable, typeof(DBNull)),
                                                                            Expression.ReferenceEqual(objVariable, Expression.Constant(null, objVariable.Type))),
                                                              GetExpressionForDbNullValue(entity, prop),
                                                              GetExpressionForNonNullValue(entity, new KeyValuePair<string, PropertyInfo>(property.Key, prop), objVariable)));
                    }
                }
            }

            var postprocessEntityMethod = _iAfterReadEntity.IsAssignableFrom(entity.Type) ? _postprocessEntity : entity.Type.GetMethod(nameof(IAfterReadEntity.PostProcessEntity), BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (postprocessEntityMethod != null)
            {
                expressions.Add(Expression.Call(entity, postprocessEntityMethod, dataRecordParameter));
            }

            expressions.Add(entity);

            var body = Expression.Block(new[] { entity, objVariable }, expressions);

            var action = Expression.Lambda<Func<IDataRecord, T>>(body, dataRecordParameter);

            return action.Compile();
        }

        private Expression GetExpressionForNonNullValue(ParameterExpression entity, KeyValuePair<string, PropertyInfo> propertyInfo, ParameterExpression objVariable)
        {
            var property = propertyInfo.Value;

            if (property.PropertyType == typeof(string))
            {
                if (property.GetCustomAttribute<RequiredAttribute>()?.AllowEmptyStrings == false)
                {
                    return Expression.IfThenElse(Expression.Equal(Expression.Property(Expression.Convert(objVariable, typeof(string)), _stringLength), Expression.Constant(0)),
                                                 Expression.Throw(Expression.Constant(new ValidationException($"Property {property.Name} could not contain empty string"))),
                                                 Expression.Assign(Expression.Property(entity, property), Expression.Convert(objVariable, typeof(string))));
                }
            }

            return Expression.Assign(Expression.Property(entity, property), GetConvertValueExpression(property, objVariable));
        }

        private static Expression GetExpressionForDbNullValue(ParameterExpression entity, PropertyInfo property)
        {
            var required = property.GetCustomAttribute<RequiredAttribute>();
            if (required != null)
            {
                if (required.AllowEmptyStrings && property.PropertyType == typeof(string))
                {
                    return Expression.Assign(Expression.Property(entity, property), Expression.Constant(string.Empty));
                }

                if (!string.IsNullOrWhiteSpace(required.ErrorMessage))
                {
                    return Expression.Throw(Expression.Constant(new ValidationException(required.ErrorMessage)));
                }

                return Expression.Throw(Expression.Constant(new ValidationException($"Property {property.Name} is marked as required, but no data was recieved from database")));
            }

            return Expression.Empty();
        }

        private Expression GetConvertValueExpression(PropertyInfo property, Expression value)
        {
            var type = property.PropertyType;

            var converterFactory = _typeConverters.GetOrAddValueSafe(property, p =>
            {
                var converter = Type.GetType(p.GetCustomAttribute<TypeConverterAttribute>()?.ConverterTypeName ?? "");
                if (converter == null)
                {
                    return null;
                }

                return () => (TypeConverter)Activator.CreateInstance(converter);
            });

            if (converterFactory != null)
            {
                return Expression.Call(Expression.Constant(converterFactory()), _convert, value);
            }

            return _valueConverter.GetConvertValueExpression(value, type);
        }

        Dictionary<string, ReadOnlyCollection<PropertyInfo>> ISingleEntityMetadataProvider.Properties => _properties.ToDictionary(x => x.Key, x => x.Value.AsReadOnly(), StringComparer.OrdinalIgnoreCase);
    }
}
