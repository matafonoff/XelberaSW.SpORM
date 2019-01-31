/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using XelberaSW.SpORM.Metadata;
using XelberaSW.SpORM.Utilities;

namespace XelberaSW.SpORM.Internal.DelegateBuilders
{
    abstract class AsyncDelegateBuilder : DelegateBuilderBase
    {
        protected static readonly MethodInfo _nonDispatchedSpReaderWrapperMethodInfo = typeof(DbContext).GetMethod(nameof(DbContext.InvokeStoredProcedureAsync));

        protected static readonly MethodInfo _dictAdd = typeof(IDictionary<string, object>).GetMethod(nameof(IDictionary<string, object>.Add),
                                                                                                    BindingFlags.Public | BindingFlags.Instance, null,
                                                                                                    new[] { typeof(string), typeof(object) }, null);

        /// <inheritdoc />
        protected AsyncDelegateBuilder(MethodInfo caller) : base(caller)
        { }

        protected abstract MethodInfo GetReaderWrapper(MethodContext context);

        #region Overrides of DelegateBuilderBase

        /// <inheritdoc />
        protected override Type GetDelegateType()
        {
            var func = Type.GetType($"System.Func`{Parameters.Length + 1}", true).MakeGenericType(Parameters.Select(x => x.Type).Append(ReturnType).ToArray());
            return func;
        }

        /// <inheritdoc />
        protected override BlockExpression GetMethodBody()
        {
            var methodContext = PrepareMethodContext();

            if (!methodContext.IsAsync)
            {
                throw new InvalidOperationException();
            }

            GetBodyForMethod(methodContext);

            var body = methodContext.Build();
            return body;
        }

        #endregion

        private void GetBodyForMethod(MethodContext methodContext)
        {
            ParameterExpression param;
            var ret = methodContext.Return(methodContext.TaskType);
            var task = methodContext.AddVariable(typeof(Task<IDataReader>), "task");

            var parameterConverter = StoredProcedureAttribute.GetConverter(Caller);

            if (parameterConverter == null)
            {
                throw new InvalidOperationException($"Could not get parameter converter for method {Caller.Name} of type {Caller.DeclaringType.FullName}");
            }

            if (parameterConverter is DefaultParameterConverter parameterConverterInternal)
            {
                param = parameterConverterInternal.Convert(methodContext, Parameters, Caller);
            }
            else
            {
                param = CustomConverter(methodContext, parameterConverter);
            }

            var dbConnectionParameters = Caller.GetCustomAttribute<DbConnectionParametersAttribute>();
            if (dbConnectionParameters != null)
            {
                SetRequestTimeout(methodContext, param, dbConnectionParameters);
            }

            var disp = methodContext.DispatcherMethod;

            var arguments = GetDispatcherAttributes(methodContext, param, disp);

            methodContext.Do(Expression.Assign(task,
                                               disp == null ?
                                                   Expression.Call(ThisParameter, _nonDispatchedSpReaderWrapperMethodInfo, arguments) :
                                                   Expression.Call(ThisParameter, disp, arguments)));

            var reader = GetReaderWrapper(methodContext);

            var action = reader.Invoke(null, new object[0]);
            var proxType = action.GetType();
            var prox = methodContext.AddVariable(proxType, "proxy");
            methodContext.Do(prox.Assign(action));

            var invokeProx = proxType.GetMethod("Invoke");
            methodContext.Do(ret.Assign(Expression.Call(prox, invokeProx, task)));
        }

        private List<Expression> GetDispatcherAttributes(MethodContext methodContext, ParameterExpression param, MethodInfo disp)
        {
            var arguments = new List<Expression>
            {
                methodContext.ActionNameConstant,
                param
            };

            if (disp == null ||
                disp.GetParameters().LastOrDefault()?.ParameterType == typeof(CancellationToken))
            {
                if (CancellationToken == null)
                {
                    arguments.Add(Expression.Constant(System.Threading.CancellationToken.None));
                }
                else
                {
                    arguments.Add(CancellationToken);
                }
            }

            return arguments;
        }

        private ParameterExpression CustomConverter(MethodContext methodContext, IParameterConverter parameterConverter)
        {
            var param = methodContext.AddVariable(typeof(object), "dispArgs");

            var dict = Expression.Variable(typeof(IDictionary<string, object>), "params");

            var dictInit = new List<Expression>
            {
                dict.Assign(Expression.Constant(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)))
            };

            foreach (var parameter in Parameters.Skip(1))
            {
                dictInit.Add(Expression.Call(dict, _dictAdd, Expression.Constant(parameter.Name), parameter));
            }

            dictInit.Add(Expression.Call(Expression.Constant(parameterConverter), StoredProcedureAttribute.Convert, dict));

            methodContext.Do(param.Assign(Expression.Block(param.Type, new[] { dict }, dictInit)));
            return param;
        }

        private void SetRequestTimeout(MethodContext methodContext, ParameterExpression param, IConnectionParameters dbParameters)
        {
            var propInfo = DbContextProxyHelper.GetConnectionParamsProperty(param.Type);
            if (propInfo == null)
            {
                return;
            }

            var newDbConfig = Expression.New(typeof(ConnectionParameters).GetConstructor(new[] { typeof(IConnectionParameters) }), Expression.Constant(dbParameters));
            methodContext.Do(Expression.Assign(Expression.Property(param, propInfo), newDbConfig));
        }
    }
}
