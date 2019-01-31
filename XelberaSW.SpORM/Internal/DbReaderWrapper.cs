/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace XelberaSW.SpORM.Internal
{
    class DbReaderWrapper : IDataReader
    {
        private const string ELEMENT_NAME = "Output";


        private readonly IDataReader _reader;
        private readonly DbCommand _cmd;

        private readonly Dictionary<string, int> _indexMapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly Lazy<XElement> _output;

        public DbReaderWrapper(IDataReader reader, DbCommand cmd, List<IDbDataParameter> errorParams)
        {
            _reader = reader;
            _cmd = cmd;

            var outParameters = cmd.Parameters.Cast<IDbDataParameter>()
                                   .Where(x => x.Direction != ParameterDirection.Input && !errorParams.Contains(x))
                                   .ToList();

            _output = outParameters.Any() ?
                           new Lazy<XElement>(() => GetOutput(outParameters)) :
                           new Lazy<XElement>(() => null);
        }

        private XElement GetOutput(IReadOnlyList<IDbDataParameter> parameters)
        {
            XElement root;
            if (parameters.Count == 1)
            {
                root = ToElement(parameters[0]);

                if (root.HasElements)
                {
                    if (root.Elements().Count() == 1)
                    {
                        root = root.Elements().First();
                    }
                }

                root.Name = ELEMENT_NAME;
            }
            else
            {
                root = new XElement(ELEMENT_NAME);
                foreach (var parameter in parameters)
                {
                    var param = ToElement(parameter);

                    root.Add(param);
                }
            }

            return root;
        }

        private static XElement ToElement(IDbDataParameter parameter)
        {
            var param = new XElement(parameter.ParameterName.TrimStart('@'));
            param.SetAttributeValue(nameof(IDataParameter.DbType), parameter.DbType);
            param.SetAttributeValue(nameof(IDataParameter.SourceColumn), parameter.SourceColumn);

            if (parameter.Value != null &&
                parameter.Value != DBNull.Value)
            {
                switch (parameter.DbType)
                {
                    case DbType.Xml:
                        var xml = (SqlXml)parameter.Value;
                        try
                        {
                            if (!xml.IsNull)
                            {
                                var value = XElement.Parse(xml.Value);

                                //param.SetElementValue(value.Name, 0);
                                //param.Element(value.Name).ReplaceWith(value);

                                param.AddFirst(value);
                            }
                        }
                        catch(Exception ex)
                        {
                            Trace.WriteLine(ex, "FAILED");
                        }

                        break;
                    default:
                        param.SetValue(parameter.Value);
                        break;
                }
            }

            return param;
        }

        public XElement Output => _output.Value;

        /// <inheritdoc />
        public bool GetBoolean(int i)
        {
            return _reader.GetBoolean(i);
        }

        /// <inheritdoc />
        public byte GetByte(int i)
        {
            return _reader.GetByte(i);
        }

        /// <inheritdoc />
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return _reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        /// <inheritdoc />
        public char GetChar(int i)
        {
            return _reader.GetChar(i);
        }

        /// <inheritdoc />
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        /// <inheritdoc />
        public IDataReader GetData(int i)
        {
            return _reader.GetData(i);
        }

        /// <inheritdoc />
        public string GetDataTypeName(int i)
        {
            return _reader.GetDataTypeName(i);
        }

        /// <inheritdoc />
        public DateTime GetDateTime(int i)
        {
            return _reader.GetDateTime(i);
        }

        /// <inheritdoc />
        public decimal GetDecimal(int i)
        {
            return _reader.GetDecimal(i);
        }

        /// <inheritdoc />
        public double GetDouble(int i)
        {
            return _reader.GetDouble(i);
        }

        /// <inheritdoc />
        public Type GetFieldType(int i)
        {
            return _reader.GetFieldType(i);
        }

        /// <inheritdoc />
        public float GetFloat(int i)
        {
            return _reader.GetFloat(i);
        }

        /// <inheritdoc />
        public Guid GetGuid(int i)
        {
            return _reader.GetGuid(i);
        }

        /// <inheritdoc />
        public short GetInt16(int i)
        {
            return _reader.GetInt16(i);
        }

        /// <inheritdoc />
        public int GetInt32(int i)
        {
            return _reader.GetInt32(i);
        }

        /// <inheritdoc />
        public long GetInt64(int i)
        {
            return _reader.GetInt64(i);
        }

        /// <inheritdoc />
        public string GetName(int i)
        {
            return _reader.GetName(i);
        }

        /// <inheritdoc />
        public int GetOrdinal(string name)
        {
            lock (_indexMapping)
            {
                return _indexMapping.TryGetValue(name, out var index) ? index : -1;
            }
        }

        /// <inheritdoc />
        public string GetString(int i)
        {
            return _reader.GetString(i);
        }

        /// <inheritdoc />
        public object GetValue(int i)
        {
            return _reader.GetValue(i);
        }

        /// <inheritdoc />
        public int GetValues(object[] values)
        {
            return _reader.GetValues(values);
        }

        /// <inheritdoc />
        public bool IsDBNull(int i)
        {
            return _reader.IsDBNull(i);
        }

        /// <inheritdoc />
        public int FieldCount => _reader.FieldCount;

        /// <inheritdoc />
        public object this[int i] => _reader[i];

        /// <inheritdoc />
        public object this[string name]
        {
            get
            {
                bool result;
                int index;

                lock (_indexMapping)
                {
                    result = _indexMapping.TryGetValue(name, out index);
                }

                if (result)
                {
                    return _reader[index];
                }

                return DBNull.Value;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            using (_cmd.Connection)
            {
                using (_cmd)
                {
                    _reader.Dispose();
                }

                _cmd.Connection.Close();
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            _reader.Close();
        }

        /// <inheritdoc />
        public DataTable GetSchemaTable()
        {
            return _reader.GetSchemaTable();
        }

        /// <inheritdoc />
        public bool NextResult()
        {
            lock (_indexMapping)
            {
                _indexMapping.Clear();
            }

            return _reader.NextResult();
        }

        /// <inheritdoc />
        public bool Read()
        {
            lock (_indexMapping)
            {
                var result = _reader.Read();

                if (result)
                {
                    if (_indexMapping.Count == 0)
                    {
                        for (int i = 0; i < _reader.FieldCount; i++)
                        {
                            var colName = _reader.GetName(i);
                            _indexMapping.Add(colName, i);
                        }
                    }
                }
                else
                {
                    _indexMapping.Clear();
                }

                return result;
            }
        }

        /// <inheritdoc />
        public int Depth => _reader.Depth;

        /// <inheritdoc />
        public bool IsClosed => _reader.IsClosed;

        /// <inheritdoc />
        public int RecordsAffected => _reader.RecordsAffected;
    }
}
