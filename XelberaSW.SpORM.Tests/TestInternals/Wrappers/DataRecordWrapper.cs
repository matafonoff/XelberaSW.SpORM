using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using XelberaSW.SpORM.Internal.Readers;

// ReSharper disable StaticMemberInGenericType

namespace XelberaSW.SpORM.Tests.TestInternals.Wrappers
{
    class DataRecordWrapper<T> : IDataRecord
    {
        private static readonly Dictionary<string, ReadOnlyCollection<PropertyInfo>> _properties;

        private readonly T _record;

        static DataRecordWrapper()
        {
            ISingleEntityMetadataProvider reader = new SingleModelReader<T>();

            _properties = reader.Properties;
        }

        public DataRecordWrapper(T record)
        {
            _record = record;
        }

        /// <inheritdoc />
        public bool GetBoolean(int i)
        {
            return Convert.ToBoolean(this[i]);
        }

        /// <inheritdoc />
        public byte GetByte(int i)
        {
            return Convert.ToByte(this[i]);
        }

        /// <inheritdoc />
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public char GetChar(int i)
        {
            return Convert.ToChar(this[i]);
        }

        /// <inheritdoc />
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public IDataReader GetData(int i)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public string GetDataTypeName(int i)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public DateTime GetDateTime(int i)
        {
            return Convert.ToDateTime(this[i]);
        }

        /// <inheritdoc />
        public decimal GetDecimal(int i)
        {
            return Convert.ToDecimal(this[i]);
        }

        /// <inheritdoc />
        public double GetDouble(int i)
        {
            return Convert.ToDouble(this[i]);
        }

        /// <inheritdoc />
        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public float GetFloat(int i)
        {
            return Convert.ToSingle(this[i]);
        }

        /// <inheritdoc />
        public Guid GetGuid(int i)
        {
            return new Guid(GetString(i));
        }

        /// <inheritdoc />
        public short GetInt16(int i)
        {
            return Convert.ToInt16(this[i]);
        }

        /// <inheritdoc />
        public int GetInt32(int i)
        {
            return Convert.ToInt32(this[i]);
        }

        /// <inheritdoc />
        public long GetInt64(int i)
        {
            return Convert.ToInt64(this[i]);
        }

        /// <inheritdoc />
        public string GetName(int i)
        {
            return _properties.Keys.Skip(i).FirstOrDefault();
        }

        /// <inheritdoc />
        public int GetOrdinal(string name)
        {
            return _properties.Keys.ToList().IndexOf(name);
        }

        /// <inheritdoc />
        public string GetString(int i)
        {
            return Convert.ToString(this[i]);
        }

        /// <inheritdoc />
        public object GetValue(int i)
        {
            return this[i];
        }

        /// <inheritdoc />
        public int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);

            for (int i = 0; i < count; i++)
            {
                values[i] = this[i];
            }

            return count;
        }

        /// <inheritdoc />
        public bool IsDBNull(int i)
        {
            var value = this[i];
            return value == null || value is DBNull;
        }

        /// <inheritdoc />
        public int FieldCount => _properties.Count;

        /// <inheritdoc />
        public object this[int i] => this[_properties.Keys.Skip(i).FirstOrDefault() ?? ""];

        /// <inheritdoc />
        public object this[string name] => _properties.TryGetValue(name, out var propInfo) ? propInfo.Select(x => x.GetValue(_record)).FirstOrDefault(x => x != null) : null;
    }
}
