using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;

namespace XelberaSW.SpORM.Internal.Readers
{
    class MultipleModelsReader<T> : ISingleEntityMetadataProvider, IReader<IDataReader, IEnumerable<T>>, IMultipleModelsReaderConfigurator
    {
        private readonly SingleModelReader<T> _singleModelReader ;

        public MultipleModelsReader()
        {
            _singleModelReader = new SingleModelReader<T>();
        }

        public MultipleModelsReader(IEnumerable<PropertyInfo> properties)
        {
            _singleModelReader = new SingleModelReader<T>(properties);
        }

        public IEnumerable<T> Read(IDataReader dataRecord)
        {
            if (OneRecordWasReadAlready)
            {
                yield return _singleModelReader.Read(dataRecord);
            }

            while (dataRecord.Read())
            {
                yield return _singleModelReader.Read(dataRecord);
            }
        }

        Func<IDataReader, IEnumerable<T>> IReader<IDataReader, IEnumerable<T>>.GetReader() => Read;

        /// <inheritdoc />
        Dictionary<string, ReadOnlyCollection<PropertyInfo>> ISingleEntityMetadataProvider.Properties => ((ISingleEntityMetadataProvider)_singleModelReader).Properties;

        /// <inheritdoc />
        public bool OneRecordWasReadAlready { get; set; }
    }
}