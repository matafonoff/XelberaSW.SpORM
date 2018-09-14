using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using XelberaSW.SpORM.Internal.Readers.ComplexModelInternals;
using XelberaSW.SpORM.Metadata;

// ReSharper disable StaticMemberInGenericType

namespace XelberaSW.SpORM.Internal.Readers
{
    class ComplexModelReader<TDataset> : ModelReaderBase<IDataReader, TDataset>
        where TDataset : class, new()
    {
        protected override Func<IDataReader, TDataset> CreateReader()
        {
            var customReader = CustomReaderAttribute.GetReader<TDataset>();
            if (customReader != null)
            {
                return GetCustomReader(customReader);
            }

            var realType = TypeHelper.GetRealType<TDataset>();
            var datasetType = typeof(TDataset);

            var isCollection = realType != datasetType;

            var simpleProperties = new List<PropertyInfo>();
            var entities = new List<PropertyInfo>();

            InitializeProperties(realType, simpleProperties, entities);

            if (entities.Count == 0)
            {
                return GetSimpleObjectReader(isCollection, simpleProperties, realType);
            }

            if (isCollection)
            {
                throw new NotSupportedException("TDataset seems to be a collection, but type of elements is not a POCO class");
            }

            // skipping simple properties as nonrelevant
            var readers = new ComplexEntityMetadata<TDataset>(entities);
            return GetComplexObjectReader(readers);
        }

        private Func<IDataReader, TDataset> GetSimpleObjectReader(bool isCollection, List<PropertyInfo> simpleProperties, Type realType)
        {
            if (isCollection)
            {
                return ReadCollection(simpleProperties, realType);
            }

            if (simpleProperties.Count == 0)
            {
                return reader => new TDataset();
            }

            // single row expected
            return GetSingleEntityReader(simpleProperties);
        }

        private static void InitializeProperties(Type realType, List<PropertyInfo> simpleProperties, List<PropertyInfo> entities)
        {
            foreach (var prop in realType.GetProperties())
            {
                var nestedType = prop.PropertyType;
                if (nestedType.IsSimpleType())
                {
                    simpleProperties.Add(prop);
                }
                else
                {
                    entities.Add(prop);
                }
            }
        }

        private Func<IDataReader, TDataset> GetCustomReader(CustomReaderBase customReader)
        {
            return reader =>
            {
                var dataset = new TDataset();
                customReader.Read(dataset, reader);
                return dataset;
            };
        }

        private static readonly MethodInfo _readArray = typeof(ComplexModelReader<TDataset>).GetMethod(nameof(ReadArray), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo _readList = typeof(ComplexModelReader<TDataset>).GetMethod(nameof(ReadList), BindingFlags.Static | BindingFlags.NonPublic);

        private Func<IDataReader, TDataset> ReadCollection(IList<PropertyInfo> properties, Type realType)
        {
            if (properties.Count == 0)
            {
                return _ => default(TDataset);
            }

            var readerType = typeof(MultipleModelsReader<>).MakeGenericType(realType);
            var reader = (IReader<IDataReader, IEnumerable>)Activator.CreateInstance(readerType, BindingFlags.Instance | BindingFlags.Public, null, new object[] { properties.AsEnumerable() }, null);

            MethodInfo methodToCall;

            if (realType.IsArray)
            {
                // <read multiple>.ToArray();
                methodToCall = _readArray;
            }
            else
            {
                var listOfItems = typeof(List<>).MakeGenericType(realType);
                if (typeof(TDataset).IsAssignableFrom(listOfItems) ||
                    typeof(TDataset).IsAssignableFrom(typeof(IEnumerable<>).MakeGenericType(readerType)) ||
                    typeof(TDataset).IsAssignableFrom(typeof(IEnumerable)) && typeof(TDataset) != typeof(string))
                {
                    // <read multiple>.ToList();
                    methodToCall = _readList;
                }
                else
                {
                    throw new NotSupportedException("TDataset seems to be a collection, but is not an Array and it does not implement ICollection<T>");
                }
            }

            var dataReader = Expression.Parameter(typeof(IDataReader), "dataReader");

            methodToCall = methodToCall.MakeGenericMethod(realType);

            var lambda = Expression.Lambda<Func<IDataReader, TDataset>>(Expression.Convert(Expression.Call(methodToCall, Expression.Constant(reader), dataReader), typeof(TDataset)), dataReader);
            var wrapper = lambda.Compile();

            return x =>
            {
                using (x)
                {
                    return wrapper(x);
                }
            };
        }

        private static List<T> ReadList<T>(IReader<IDataReader, IEnumerable<T>> reader, IDataReader dataReader)
        {
            return reader.Read(dataReader).ToList();
        }
        private static T[] ReadArray<T>(IReader<IDataReader, IEnumerable<T>> reader, IDataReader dataReader)
        {
            return reader.Read(dataReader).ToArray();
        }

        private static Func<IDataReader, TDataset> GetSingleEntityReader(List<PropertyInfo> properties)
        {
            var reader = new SingleModelReader<TDataset>(properties);

            return x =>
            {
                using (x)
                {
                    if (x.Read())
                    {
                        return reader.Read(x);
                    }

                    return default(TDataset);
                }
            };
        }

        private static Func<IDataReader, TDataset> GetComplexObjectReader(ComplexEntityMetadata<TDataset> readers)
        {
            return dataReader =>
            {
                var dataset = new TDataset();
                var dataTableIndex = -1;
                do
                {
                    dataTableIndex++;

                    if (dataReader.Read())
                    {
                        var provider = readers.GetReaderProvider(dataReader, dataTableIndex);
                        if (provider == null)
                        {
                            continue;
                        }

                        var reader = provider.GetReader();

                        reader(dataReader, dataset);
                    }

                } while (dataReader.NextResult());

                return dataset;
            };
        }
    }
}
