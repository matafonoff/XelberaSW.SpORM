/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Data;

namespace XelberaSW.SpORM.Utilities
{
    public static class DatabaseExtensions
    {
        public static IEnumerable<string> GetColumns(this IDataReader reader)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                yield return (reader.GetName(i) ?? "").ToLower();
            }
        }

        public static IDbDataParameter AddParameter(this IDbCommand command)
        {
            var param = command.CreateParameter();

            command.Parameters.Add(param);

            return param;
        }

        public static IDbDataParameter AddParameter(this IDbCommand command, string name)
        {
            return command.AddParameter().HasName(name);
        }

        public static IDbDataParameter HasName(this IDbDataParameter parameter, string name)
        {
            parameter.ParameterName = name;

            return parameter;
        }

        public static IDbDataParameter HasType(this IDbDataParameter parameter, DbType type)
        {
            parameter.DbType = type;

            return parameter;
        }

        public static IDbDataParameter HasSize(this IDbDataParameter parameter, int size)
        {
            parameter.Size = size;

            return parameter;
        }

        public static void WithValue(this IDbDataParameter parameter, object value)
        {
            if (value == null)
            {
                parameter.Value = DBNull.Value;
            }
            else if (parameter.DbType == DbType.Xml)
            {
                parameter.Value = CustomXmlSerializer.Instance.ToSqlXml(value, "Param");
            }
            else
            {
                parameter.Value = value;
            }
        }

        public static IDbDataParameter IsOutput(this IDbDataParameter parameter)
        {
            parameter.Direction = ParameterDirection.Output;
            return parameter;
        }
    }
}
