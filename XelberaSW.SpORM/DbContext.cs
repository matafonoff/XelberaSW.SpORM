/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XelberaSW.SpORM.Internal;
using XelberaSW.SpORM.Utilities;

namespace XelberaSW.SpORM
{
    public abstract class DbContext : IDisposable
    {
        private readonly DbContextParameters _parameters;

        private static readonly DbContextProxyHelper _dbContextHelper = new DbContextProxyHelper();


        private readonly ILogger _logger;

        protected DbContext(DbContextParameters parameters)
        {
            if (parameters.ConnectionType == null)
            {
                throw new ArgumentException(nameof(parameters));
            }

            _parameters = parameters;

            _logger = _parameters.LoggerFactory.CreateLogger(GetType().FullName);
        }

        public IDataReader InvokeStoredProcedure(string procedureName, object arguments)
        {
            return InvokeStoredProcedureAsync(procedureName, arguments, CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task<IDataReader> InvokeStoredProcedureAsync(string procedureName, object arguments, CancellationToken token)
        {
            var storedProcedure = new StoredProcedure(_parameters.LoggerFactory.CreateLogger(typeof(StoredProcedure).FullName), _parameters, procedureName);

            return storedProcedure.Invoke(arguments, token);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization | MethodImplOptions.Synchronized)]
        protected T ExecStoredProcedure<T>(params object[] arguments)
        {
            using (_logger.BeginScope(nameof(ExecStoredProcedure)))
            {
                try
                {
                    _logger.LogDebug("Searching for calling method");
                    var stackFrames = new StackTrace().GetFrames();

                    var caller = stackFrames.Select(x => (MethodInfo)x.GetMethod())
                                            .Skip(1)
                                            .FirstOrDefault(x => typeof(DbContext).IsAssignableFrom(x.DeclaringType));

                    if (caller == null)
                    {
                        throw new MissingMethodException("Caller procedure could not be found, probably it was removed by optimizer. Check if caller method is marked by [MethodImpl(MethodImplOptions.NoInlining)]");
                    }

                    var fullName = $"{caller.DeclaringType.FullName}.{caller.Name}";
                    lock (SyncRoot.Get(fullName))
                    {
                        _logger.LogDebug($"Found: {fullName}");

                        if (arguments.Length != caller.GetParameters().Length)
                        {
                            throw new InvalidOperationException("ExecStoredProcedure is executed with invalid number of arguments");
                        }

                        var logger = _parameters.LoggerFactory.CreateLogger($"{nameof(DbContextProxyHelper)}.{nameof(DbContextProxyHelper.CreateProxy)}");
                        var callerProxyMethod = _dbContextHelper.CreateProxy(caller, logger);

                        _logger.LogTrace("Calling proxy method");
                        var result = (T)callerProxyMethod.Invoke(null, arguments.Prepend(this).ToArray());
                        return result;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    throw;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            { }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
