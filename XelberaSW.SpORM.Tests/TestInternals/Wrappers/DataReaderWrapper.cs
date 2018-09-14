using System;
using System.Collections.Generic;
using System.Data;

namespace XelberaSW.SpORM.Tests.TestInternals.Wrappers
{
    class DataReaderWrapper<TElement> : IDataReader
    {
        private readonly List<TElement> _items = new List<TElement>();

        private IDataRecord _record;

        private int _index = -1;

        public DataReaderWrapper(IEnumerable<TElement> elements)
        {
            _items.AddRange(elements);
        }

        private TElement GetCurrentElement()
        {
            if (_index < -1)
            {
                throw new ObjectDisposedException(nameof(DataReaderWrapper<TElement>));
            }

            if (_index < 0 || _index >= _items.Count)
            {
                return default(TElement);
            }

            return _items[_index];
        }

        #region IDataRecord
        /// <inheritdoc />
        bool IDataRecord.GetBoolean(int i)
        {
            return _record.GetBoolean(i);
        }

        /// <inheritdoc />
        byte IDataRecord.GetByte(int i)
        {
            return _record.GetByte(i);
        }

        /// <inheritdoc />
        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return _record.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        /// <inheritdoc />
        char IDataRecord.GetChar(int i)
        {
            return _record.GetChar(i);
        }

        /// <inheritdoc />
        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _record.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        /// <inheritdoc />
        IDataReader IDataRecord.GetData(int i)
        {
            return _record?.GetData(i);
        }

        /// <inheritdoc />
        string IDataRecord.GetDataTypeName(int i)
        {
            return _record?.GetDataTypeName(i);
        }

        /// <inheritdoc />
        DateTime IDataRecord.GetDateTime(int i)
        {
            return _record.GetDateTime(i);
        }

        /// <inheritdoc />
        decimal IDataRecord.GetDecimal(int i)
        {
            return _record.GetDecimal(i);
        }

        /// <inheritdoc />
        double IDataRecord.GetDouble(int i)
        {
            return _record.GetDouble(i);
        }

        /// <inheritdoc />
        Type IDataRecord.GetFieldType(int i)
        {
            return _record?.GetFieldType(i);
        }

        /// <inheritdoc />
        float IDataRecord.GetFloat(int i)
        {
            return _record.GetFloat(i);
        }

        /// <inheritdoc />
        Guid IDataRecord.GetGuid(int i)
        {
            return _record.GetGuid(i);
        }

        /// <inheritdoc />
        short IDataRecord.GetInt16(int i)
        {
            return _record.GetInt16(i);
        }

        /// <inheritdoc />
        int IDataRecord.GetInt32(int i)
        {
            return _record.GetInt32(i);
        }

        /// <inheritdoc />
        long IDataRecord.GetInt64(int i)
        {
            return _record.GetInt64(i);
        }

        /// <inheritdoc />
        string IDataRecord.GetName(int i)
        {
            return _record?.GetName(i);
        }

        /// <inheritdoc />
        int IDataRecord.GetOrdinal(string name)
        {
            return _record?.GetOrdinal(name) ?? -1;
        }

        /// <inheritdoc />
        string IDataRecord.GetString(int i)
        {
            return _record?.GetString(i);
        }

        /// <inheritdoc />
        object IDataRecord.GetValue(int i)
        {
            return _record?.GetValue(i);
        }

        /// <inheritdoc />
        int IDataRecord.GetValues(object[] values)
        {
            return _record?.GetValues(values) ?? -1;
        }

        /// <inheritdoc />
        bool IDataRecord.IsDBNull(int i)
        {
            return _record?.IsDBNull(i) ?? true;
        }

        /// <inheritdoc />
        int IDataRecord.FieldCount => _record?.FieldCount ?? 0;

        /// <inheritdoc />
        object IDataRecord.this[int i] => _record?[i];

        /// <inheritdoc />
        object IDataRecord.this[string name] => _record?[name];
        #endregion

        /// <inheritdoc />
        public void Dispose()
        {
            Close();
        }

        /// <inheritdoc />
        public void Close()
        {
            _items.Clear();
            _index = -2;
        }

        /// <inheritdoc />
        DataTable IDataReader.GetSchemaTable()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public bool NextResult()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public bool Read()
        {
            _record = null;

            if (_index < _items.Count - 1)
            {
                _index++;

                var item = GetCurrentElement();
                _record = new DataRecordWrapper<TElement>(item);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public int Depth => 1;

        /// <inheritdoc />
        bool IDataReader.IsClosed => false;

        /// <inheritdoc />
        int IDataReader.RecordsAffected => _items.Count;
    }
}