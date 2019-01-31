/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Internal.Readers.ComplexModelInternals
{
    class ReaderFactory<TDataset>
    {
        private readonly PropertyInfo _property;
        private readonly object _entityReader;
        private readonly Lazy<Action<IDataReader, TDataset>> _reader;
        private static readonly MethodInfo _collectionCleatorGeneric = typeof(ComplexEntityMetadata<TDataset>).GetMethod(nameof(CreateCollection), BindingFlags.Static | BindingFlags.NonPublic);

        public ReaderFactory(PropertyInfo property)
        {
            if (!property.CanWrite)
            {
                throw new InvalidOperationException($"Property {property.Name} of type {property.DeclaringType.FullName} must have public set accessor");
            }

            DataTableIndex = (property.GetCustomAttribute<DataTableIndexAttribute>()?.Index).GetValueOrDefault(-1);

            _property = property;

            EntityType = TypeHelper.GetRealType(_property.PropertyType);


            if (!IsSingleEntity)
            {
                EnumerableOfEntitiesType = typeof(IEnumerable<>).MakeGenericType(EntityType);
            }

            var type = (IsSingleEntity ?
                            typeof(SingleModelReader<>) :
                            typeof(MultipleModelsReader<>)).MakeGenericType(EntityType);

            _entityReader = Activator.CreateInstance(type);

            if (!IsSingleEntity)
            {
                ((IMultipleModelsReaderConfigurator)_entityReader).OneRecordWasReadAlready = true;
            }

            var meta = (ISingleEntityMetadataProvider)_entityReader;
            Columns = meta.Properties.Keys.OrderBy(x => x).ToArray();
            Token = ComplexEntityMetadata<TDataset>.GetMetadataToken(meta.Properties.Keys);

            _reader = new Lazy<Action<IDataReader, TDataset>>(GetReaderInternal);
        }

        public int DataTableIndex { get; }

        public bool IsSingleEntity => EntityType == _property.PropertyType;

        public Type EntityType { get; }
        public Type EnumerableOfEntitiesType { get; }

        public string Token { get; }
        public string[] Columns { get; }
        public Type PropertyType => _property.PropertyType;

        public Action<IDataReader, TDataset> GetReader() => _reader.Value;

        private Action<IDataReader, TDataset> GetReaderInternal()
        {
            var dataReader = Expression.Parameter(typeof(IDataReader), "dataReader");
            var dataset = Expression.Parameter(typeof(TDataset), "dataset");

            if (IsSingleEntity)
            {
                return GetSingleEntityReader(dataReader, dataset);
            }

            var propertyType = _property.PropertyType;
            if (propertyType.IsArray)
            {
                return GetArrayReader(dataReader, dataset);
            }

            return GetCollectionReader(dataReader, dataset);
        }

        private Action<IDataReader, TDataset> GetSingleEntityReader(ParameterExpression dataReader, ParameterExpression dataset)
        {
            var expressions = InitializeReader(out var singleEntityReader, out var read);

            expressions.Add(Expression.Assign(Expression.Property(dataset, _property), Expression.Call(singleEntityReader, read, Expression.Convert(dataReader, typeof(IDataRecord)))));

            return Compile(expressions, dataReader, dataset, singleEntityReader);
        }

        private Action<IDataReader, TDataset> GetCollectionReader(ParameterExpression dataReader, ParameterExpression dataset)
        {
            var listType = typeof(List<>).MakeGenericType(EntityType);
            if (_property.PropertyType.IsAssignableFrom(listType))
            {
                return GetListReader(listType, dataReader, dataset);
            }

            if (_property.PropertyType.IsAbstract)
            {
                throw new NotSupportedException();
            }

            return GetCollectionReader(_property.PropertyType, dataReader, dataset);
        }

        private Action<IDataReader, TDataset> GetCollectionReader(Type collectionType, ParameterExpression dataReader, ParameterExpression dataset)
        {
            var expressions = InitializeReader(out var multipleEntityReader, out var _);


            var collectionCleator = _collectionCleatorGeneric.MakeGenericMethod(collectionType, EntityType);

            var createCollection = Expression.Call(collectionCleator, multipleEntityReader, dataReader);

            expressions.Add(Expression.Assign(Expression.Property(dataset, _property), createCollection));

            return Compile(expressions, dataReader, dataset, multipleEntityReader);
        }

        private Action<IDataReader, TDataset> GetListReader(Type listType, ParameterExpression dataReader, ParameterExpression dataset)
        {
            var expressions = InitializeReader(out var multipleEntityReader, out var read);
            var readMultipleEntities = Expression.Call(multipleEntityReader, read, dataReader);

            var ctor = listType.GetConstructor(new[] { EnumerableOfEntitiesType });
            var createList = Expression.New(ctor, readMultipleEntities);

            expressions.Add(Expression.Assign(Expression.Property(dataset, _property), createList));

            return Compile(expressions, dataReader, dataset, multipleEntityReader);
        }

        private Action<IDataReader, TDataset> GetArrayReader(ParameterExpression dataReader, ParameterExpression dataset)
        {
            var expressions = InitializeReader(out var multipleEntityReader, out var read);

            var toArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(EntityType);

            expressions.Add(Expression.Assign(Expression.Property(dataset, _property), Expression.Call(toArray, Expression.Call(multipleEntityReader, read, dataReader))));

            return Compile(expressions, dataReader, dataset, multipleEntityReader);
        }


        private static TCollection CreateCollection<TCollection, TElement>(IReader<IDataReader, IEnumerable<TElement>> reader, IDataReader dataReader)
            where TCollection : ICollection<TElement>, new()
        {
            var collection = new TCollection();

            foreach (TElement item in reader.Read(dataReader))
            {
                collection.Add(item);
            }

            return collection;
        }

        private Action<IDataReader, TDataset> Compile(IEnumerable<Expression> expressions, ParameterExpression dataReader, ParameterExpression dataset, ParameterExpression entityReader, params ParameterExpression[] localVariables)
        {
            var body = Expression.Block(localVariables.Append(entityReader), expressions.Append(Expression.Empty()));

            var lambda = Expression.Lambda<Action<IDataReader, TDataset>>(body, dataReader, dataset);
            return lambda.Compile();
        }

        private List<Expression> InitializeReader(out ParameterExpression entityReader, out MethodInfo read)
        {
            var typeOfReader = typeof(IReader<,>);

            var singleEntityReaderType = IsSingleEntity ?
                                             typeOfReader.MakeGenericType(typeof(IDataRecord), EntityType) :
                                             typeOfReader.MakeGenericType(typeof(IDataReader), EnumerableOfEntitiesType);

            read = singleEntityReaderType.GetMethod(nameof(IReader<IDataReader, object>.Read));

            entityReader = Expression.Variable(singleEntityReaderType, nameof(entityReader));
            return new List<Expression>
            {
                Expression.Assign(entityReader, Expression.Convert(Expression.Constant(_entityReader), singleEntityReaderType))
            };
        }

        public float GetRate(string[] columns)
        {
            if (!Columns.Any())
            {
                return 0;
            }

            float totalColumnsInModel = Columns.Length;
            float columnsInModel = columns.Count(x => Columns.Contains(x, StringComparer.OrdinalIgnoreCase));

            return columnsInModel / totalColumnsInModel;
        }
    }
}
