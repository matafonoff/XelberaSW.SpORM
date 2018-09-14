using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using XelberaSW.SpORM.Metadata;
using XelberaSW.SpORM.Utilities;

namespace XelberaSW.SpORM.Internal.Readers.ComplexModelInternals
{
    class ComplexEntityMetadata<TDataset>
    {
        private readonly Dictionary<Type, ReaderFactory<TDataset>> _allReaderFactories = new Dictionary<Type, ReaderFactory<TDataset>>();
        private readonly Dictionary<int, Dictionary<Type, ReaderFactory<TDataset>>> _readerFactories = new Dictionary<int, Dictionary<Type, ReaderFactory<TDataset>>>();
        private readonly Dictionary<Type, string[]> _columns = new Dictionary<Type, string[]>();
        private readonly Dictionary<Type, Action<IDataReader, TDataset>> _readers = new Dictionary<Type, Action<IDataReader, TDataset>>();
        private readonly Range[] _skipDataTables;

        private class Range : IEquatable<Range>, IComparable<Range>
        {

            public Range(int from, int count)
            {
                if (count < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                Count = count;
                From = from;
            }

            public int From { get; }

            public int Count { get; }

            public uint To => (uint)From + (uint)Count - 1;

            public bool Contains(int value)
            {
                return value >= From && value <= To;
            }

            #region Equality members

            /// <inheritdoc />
            public bool Equals(Range other)
            {
                return From == other.From && Count == other.Count;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;

                return obj is Range range && Equals(range);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    return (From * 397) ^ Count;
                }
            }

            #endregion

            #region Relational members

            /// <inheritdoc />
            public int CompareTo(Range other)
            {
                return From.CompareTo(other.From);
            }

            #endregion
        }

        public ComplexEntityMetadata(IEnumerable<PropertyInfo> properties)
        {
            _skipDataTables = typeof(TDataset).GetCustomAttributes<IgnoreDataTableRangeAttribute>().Select(x => new Range(x.Index, x.Count))
                                              .Distinct()
                                              .OrderBy(x => x)
                                              .ToArray();

            foreach (var propertyGroup in properties.Select(x => new ReaderFactory<TDataset>(x))
                                                    .GroupBy(x => x.Columns.Length))
            {
                var colCount = propertyGroup.Key;

                var items = new Dictionary<Type, ReaderFactory<TDataset>>();

                foreach (var readerFactory in propertyGroup)
                {
                    var entityType = readerFactory.PropertyType;
                    var reader = readerFactory.GetReader();
                    var columns = readerFactory.Columns;

                    if (readerFactory.DataTableIndex != -1)
                    {
                        items.Add(entityType, readerFactory);

                        _readers.Add(entityType, reader);
                        _columns.Add(entityType, columns);
                    }

                    _allReaderFactories.Add(entityType, readerFactory);
                }

                _readerFactories.Add(colCount, items);
            }
        }

        private static string[] GetColumns(IDataReader reader)
        {
            return reader.GetColumns()
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                         .ToArray();
        }

        public static string GetMetadataToken(IDataReader reader)
        {
            return GetMetadataToken(GetColumns(reader));
        }

        public static string GetMetadataToken(IEnumerable<string> columnNames)
        {
            return string.Join(",", (columnNames ?? new string[0]).Select(x => x.ToLower())
                                                                  .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        public ReaderFactory<TDataset> GetReaderProvider(IDataReader dataReader, int datatableIndex)
        {
            if (_skipDataTables.Any(x => x.Contains(datatableIndex)))
            {
                return null;
            }

            if (datatableIndex >= 0)
            {
                var readerFactory = _allReaderFactories.Values.FirstOrDefault(x => x.DataTableIndex == datatableIndex);
                if (readerFactory != null)
                {
                    return readerFactory;
                }
            }
            var reader = GetReaderInternal(dataReader);

            return reader;
        }

        private ReaderFactory<TDataset> GetReaderInternal(IDataReader dataReader)
        {
            if (_readerFactories.TryGetValue(dataReader.FieldCount, out var readers))
            {
                var token = GetMetadataToken(dataReader);
                var reader = readers.SingleOrDefault(x => x.Value.Token == token);
                if (reader.Key != null &&
                    reader.Value != null)
                {
                    return reader.Value;
                }
            }

            var customReader = TryFindReader(dataReader);
            if (customReader != null)
            {
                var cache = _readerFactories.GetOrAddValueSafe(dataReader.FieldCount, x => new Dictionary<Type, ReaderFactory<TDataset>>());

                // TODO Possible performance issue
                cache[customReader.PropertyType] = customReader;
            }

            return customReader;
        }

        private ReaderFactory<TDataset> TryFindReader(IDataReader dataReader)
        {
            var columns = GetColumns(dataReader);

            if (columns.Length > 0)
            {
                var data = _allReaderFactories.Where(x => x.Value.DataTableIndex == -1)
                                              .OrderByDescending(x => x.Value.GetRate(columns))
                                              .FirstOrDefault();

                if (data.Key != null &&
                    data.Value != null)
                {
                    return data.Value;
                }
            }

            Trace.WriteLine($"Could not find reader for dataset of type {typeof(TDataset).FullName}. DataReader contains following columns: {string.Join(", ", GetColumns(dataReader))}");
            return null;
        }

    }
}
