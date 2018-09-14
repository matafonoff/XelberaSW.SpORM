using System;
using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace XelberaSW.SpORM
{
    public class DbContextParameters
    {
        public ILoggerFactory LoggerFactory { get; }

        public string ConnectionString { get; set; }

        public Type ConnectionType { get; private set; } = Type.GetType("System.Data.SqlClient.SqlConnection, System.Data.SqlClient");

        public DbContextParameters(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        public DbContextParameters UseDbConnection<T>()
            where T : DbConnection
        {
            ConnectionType = typeof(T);

            return this;
        }
    }
}