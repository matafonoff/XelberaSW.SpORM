/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;

namespace XelberaSW.SpORM.Internal.Readers
{
    abstract class ModelReaderBase<TSrc, TModel> : IReader<TSrc, TModel>
    //where TSrc : IDataRecord
    {
        private static Lazy<Func<TSrc, TModel>> _reader;

        protected ModelReaderBase()
        {
            _reader = new Lazy<Func<TSrc, TModel>>(CreateReader);
        }

        /// <inheritdoc />
        public TModel Read(TSrc source)
        {
            return GetReader()(source);
        }

        /// <inheritdoc />
        public virtual Func<TSrc, TModel> GetReader() => _reader.Value;

        protected abstract Func<TSrc, TModel> CreateReader();
    }
}
