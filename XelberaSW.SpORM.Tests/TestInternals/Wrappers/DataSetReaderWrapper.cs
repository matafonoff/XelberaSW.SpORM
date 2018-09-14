using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace XelberaSW.SpORM.Tests.TestInternals.Wrappers
{
    class DataSetReaderWrapper : IDataReader
    {
        private readonly List<List<object>> _recordsets = new List<List<object>>();
        private List<object> _items;

        private IDataRecord _record;

        private int _index = -1;
        private int _recordsetIndex = -1;

        public DataSetReaderWrapper(object obj)
        {
            

            NextResult();
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
            _recordsetIndex = -1;
            _record = null;
        }

        /// <inheritdoc />
        DataTable IDataReader.GetSchemaTable()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public bool NextResult()
        {
            _items = null;
            _record = null;

            if (_recordsetIndex < _recordsets.Count - 1)
            {
                _recordsetIndex++;

                _items = _recordsets[_recordsetIndex];

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool Read()
        {
            if (_items == null)
            {
                return false;
            }

            _record = null;

            if (_index < _items.Count - 1)
            {
                _index++;

                var item = _items[_index];

                var elemType = typeof(DataRecordWrapper<>).MakeGenericType(item.GetType());

                _record = (IDataRecord)Activator.CreateInstance(elemType, item);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public int Depth => 2;

        /// <inheritdoc />
        bool IDataReader.IsClosed => false;

        /// <inheritdoc />
        int IDataReader.RecordsAffected => _recordsets.Sum(x => x.Count);
    }
}