using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XelberaSW.SpORM.Metadata;
using XelberaSW.SpORM.Utilities;

namespace XelberaSW.SpORM.Internal
{
    class StoredProcedure
    {

        private static readonly Dictionary<Type, Func<DbConnection>> _dbConnectionFactories = new Dictionary<Type, Func<DbConnection>>();
        private static readonly Dictionary<Type, Action<object, DbCommand, List<IDbDataParameter>>> _propInitializers = new Dictionary<Type, Action<object, DbCommand, List<IDbDataParameter>>>();

        private static readonly MethodInfo _isAnonymousType = typeof(Utilities.Extensions).GetMethod(nameof(Utilities.Extensions.IsAnonymousType));
        private static readonly PropertyInfo _connectionParameters = typeof(IGeneratedArgumentsContainer).GetProperty(nameof(IGeneratedArgumentsContainer.ConnectionParameters));
        private static readonly MethodInfo _applyCommandParameters = typeof(IConnectionParametersProcessor).GetMethod(nameof(IConnectionParametersProcessor.Apply));

        private static readonly MethodInfo _addParam = typeof(DatabaseExtensions).GetMethod(nameof(DatabaseExtensions.AddParameter), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(IDbCommand), typeof(string) }, null);
        private static readonly MethodInfo _withValue = typeof(DatabaseExtensions).GetMethod(nameof(DatabaseExtensions.WithValue), BindingFlags.Static | BindingFlags.Public);
        private static readonly MethodInfo _listAdd = typeof(List<IDbDataParameter>).GetMethod(nameof(List<IDbDataParameter>.Add));

        private static readonly PropertyInfo _idbDataParameterDirection = typeof(IDataParameter).GetProperty(nameof(IDbDataParameter.Direction));
        private static readonly PropertyInfo _idbDataParameterDbType = typeof(IDataParameter).GetProperty(nameof(IDbDataParameter.DbType));
        private static readonly PropertyInfo _idbDataParameterSize = typeof(IDbDataParameter).GetProperty(nameof(IDbDataParameter.Size));

        private readonly ILogger _logger;
        private readonly string _procedureName;
        private readonly Type _connectionType;
        private readonly string _connectionString;

        public StoredProcedure(ILogger logger, DbContextParameters parameters, string procedureName)
        {
            _logger = logger;
            _procedureName = procedureName;

            _connectionType = parameters.ConnectionType;
            _connectionString = parameters.ConnectionString;
        }

        private async Task<DbConnection> GetConnection(CancellationToken token)
        {
            using (_logger.BeginScope($"{nameof(StoredProcedure)}.{nameof(GetConnection)}"))
            {
                _logger.LogTrace("Connector type: " + _connectionType.FullName);

                var createConnection = _dbConnectionFactories.GetOrAddValueSafe(_connectionType, x =>
                {
                    var newExpr = Expression.New(x);
                    var cast = Expression.TypeAs(newExpr, typeof(DbConnection));

                    var del = Expression.Lambda<Func<DbConnection>>(cast, Enumerable.Empty<ParameterExpression>()).Compile();
                    return del;
                });

                var connection = createConnection();

                _logger.LogTrace("Connection string: " + _connectionString);

                connection.ConnectionString = _connectionString;
                await connection.OpenAsync(token);

                return connection;
            }
        }

        public async Task<IDataReader> Invoke(object arguments, CancellationToken token)
        {
            using (_logger.BeginScope($"{nameof(StoredProcedure)}.{nameof(Invoke)}"))
            {
                try
                {
                    var connection = await GetConnection(token);

                    var cmd = connection.CreateCommand();

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = _procedureName;

                    var errorParams = InitializeDbDataParameters(arguments, cmd);
                    token.ThrowIfCancellationRequested();

                    _logger.LogTrace($"Executing stored procedure '{_procedureName}'");

                    var data = await cmd.ExecuteReaderAsync(token);

                    if (errorParams.Any())
                    {
                        var errorMessages = errorParams.Select(x => x.Value?.ToString())
                                                       .Where(x => !string.IsNullOrWhiteSpace(x))
                                                       .ToList();

                        if (errorMessages.Count > 0)
                        {
                            _logger.LogTrace("Error from database recieved!");
                            if (errorMessages.Count == 1)
                            {
                                throw new SpExecutionException(errorMessages[0]);
                            }

                            throw new SpExecutionException(errorMessages.Select(x => new SpExecutionException(x)));
                        }
                    }

                    var reader = new DbReaderWrapper(data, cmd, errorParams);

                    return reader;
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, e.Message);
                    throw;
                }
            }
        }



        private List<IDbDataParameter> InitializeDbDataParameters(object arguments, DbCommand cmd)
        {
            var errorParams = new List<IDbDataParameter>();

            if (arguments is IDbParametersInitializer value)
            {
                _logger.LogTrace($"Arguments object implements {nameof(IDbParametersInitializer)} interface. No automatically created lambda expression will be used.");

                value.Initialize(cmd, errorParams);
            }
            else
            {
                _propInitializers.GetOrAddValueSafe(arguments.GetType(), GetDbParametersInitializer)(arguments, cmd, errorParams);
            }

            return errorParams;
        }

        // ReSharper disable once UnusedMember.Local
        private void Trace(ICollection<Expression> expr, Expression obj, string header)
        {
            var mi = typeof(Trace).GetMethod(nameof(System.Diagnostics.Trace.WriteLine), new[] { typeof(object), typeof(string) });
            expr.Add(Expression.Call(mi, Expression.Convert(obj, typeof(object)), Expression.Constant(header)));
        }

        private Action<object, DbCommand, List<IDbDataParameter>> GetDbParametersInitializer(Type t)
        {
            using (_logger.BeginScope($"{nameof(StoredProcedure)}.{nameof(GetDbParametersInitializer)}({t.FullName})"))
            {
                _logger.LogTrace("Creating arguments builder lambda expression");
                try
                {
                    var argumentsParam = Expression.Parameter(typeof(object), "args");
                    var cmd = Expression.Parameter(typeof(DbCommand), "cmd");
                    var errorParams = Expression.Parameter(typeof(List<IDbDataParameter>), "errorParams");

                    var arguments = Expression.Variable(t, "arguments");
                    var param = Expression.Variable(typeof(IDbDataParameter), "param");

                    using (_logger.BeginScope("lambda_expression"))
                    {
                        var expressions = new List<Expression>
                        {
                            arguments.Assign(Expression.Convert(argumentsParam, t))
                        };



                        foreach (var property in t.GetProperties())
                        {
                            if (property.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                            {
                                _logger.LogTrace($"Skipping property {property.Name} due to {nameof(IgnoreDataMemberAttribute)} attribute");

                                continue;
                            }

                            var spParameter = property.GetCustomAttribute<SpParameterAttribute>() ?? new SpParameterAttribute(property.Name);

                            expressions.Add(param.Assign(Expression.Call(_addParam, cmd, Expression.Constant(spParameter.Name))));
                            expressions.Add(Expression.Assign(Expression.Property(param, _idbDataParameterDirection), Expression.Constant(spParameter.Direction)));
                            expressions.Add(Expression.Assign(Expression.Property(param, _idbDataParameterDbType), Expression.Constant(spParameter.Type)));
                            expressions.Add(Expression.Assign(Expression.Property(param, _idbDataParameterSize), Expression.Constant(spParameter.Size)));

                            Expression value = Expression.Property(arguments, property);
                            if (property.PropertyType.IsValueType)
                            {
                                value = Expression.Convert(value, typeof(object));
                            }

                            expressions.Add(Expression.Call(_withValue, param, value));

                            if (property.GetCustomAttribute<SpErrorInformationAttribute>() != null &&
                                spParameter.Direction != ParameterDirection.Input)
                            {
                                expressions.Add(Expression.Call(errorParams, _listAdd, param));
                            }

                            if (!property.PropertyType.IsSimpleType())
                            {
                                /*
                                 * if (arguments<property> != null &&
                                 *     !arguments<property>.GetType().IsAnonymousType() &&
                                 *     arguments<property> is IGeneratedArgumentsContainer &&
                                 *     ((IGeneratedArgumentsContainer)arguments<property>).ConnectionParameters != null) {
                                 *      ((IGeneratedArgumentsContainer)arguments<property>).ConnectionParameters.Apply(command);
                                 * }
                                 */

                                var genType = typeof(IGeneratedArgumentsContainer);
                                var propExpr = Expression.Property(arguments, property);

                                var connParameters = Expression.Property(Expression.Convert(propExpr, genType), _connectionParameters);


                                var ifExpr = Expression.And(Expression.Not(Expression.Call(_isAnonymousType, Expression.Call(propExpr, typeof(object).GetMethod(nameof(GetType))))),
                                                            Expression.TypeIs(propExpr, genType));

                                if (!property.PropertyType.IsValueType)
                                {
                                    ifExpr = Expression.And(Expression.ReferenceNotEqual(propExpr, Expression.Constant(null)), ifExpr);
                                }

                                //Trace(expressions, ifExpr, "!!!!!!!!!!");

                                var expr =
                                Expression.IfThen(ifExpr,
                                                  Expression.IfThen(Expression.ReferenceNotEqual(connParameters, Expression.Constant(null)),
                                                                    Expression.Call(connParameters, _applyCommandParameters, cmd)));


                                expressions.Add(expr);
                            }

                        }

                        var body = Expression.Block(new[]
                        {
                            arguments,
                            param
                        }, expressions);

                        var lambda = Expression.Lambda<Action<object, DbCommand, List<IDbDataParameter>>>(body, argumentsParam, cmd, errorParams);
                        return lambda.Compile();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, e.Message);
                    throw;
                }
            }
        }
    }
}