/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Internal.DelegateBuilders
{
    abstract class DelegateBuilderBase
    {
        protected MethodInfo Caller { get; }
        protected ParameterExpression ThisParameter { get; }
        protected ParameterExpression[] Parameters { get; }

        protected static MethodInfo _Dispose = typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose));

        protected Type ReturnType => Caller.ReturnType;

        protected ParameterExpression CancellationToken { get; }

        protected DelegateBuilderBase(MethodInfo caller)
        {
            Caller = caller;

            ThisParameter = Expression.Parameter(caller.DeclaringType, "this");

            var parameters = Enumerable.Repeat(ThisParameter, 1).Concat(caller.GetParameters().Select(x => Expression.Parameter(x.ParameterType, x.Name))).ToArray();

            if (parameters[parameters.Length - 1].Type == typeof(CancellationToken))
            {
                CancellationToken = parameters[parameters.Length - 1];
               // Array.Resize(ref parameters, parameters.Length - 1);
            }

            Parameters = parameters;
        }

        public static Delegate Build(MethodInfo caller)
        {
            if (caller.ReturnType == typeof(void))
            {
                return new ActionBuilder(caller).BuildInternal();
            }

            if (caller.ReturnType == typeof(Task))
            {
                return new AsyncActionBuilder(caller).BuildInternal();
            }

            if (typeof(Task).IsAssignableFrom(caller.ReturnType))
            {
                return new AsyncFuncBuilder(caller).BuildInternal();
            }

            return new FuncBuilder(caller).BuildInternal();
        }

        private Delegate BuildInternal()
        {
            var body = GetMethodBody();
            var func = GetDelegateType();

            var lambda = Expression.Lambda(func, body, Parameters);
            var method = lambda.Compile();
            return method;
        }

        protected MethodContext PrepareMethodContext()
        {
            var dispatcherMethod = (Caller.GetCustomAttribute<UseDispatcherAttribute>() ?? new UseDispatcherAttribute()).GetDispatcherMethod(Caller.DeclaringType);
            var actionName = (Caller.GetCustomAttribute<StoredProcedureAttribute>() ?? new StoredProcedureAttribute(Caller.Name)).Name;

            return new MethodContext(dispatcherMethod, actionName, ReturnType).AttachParameters(Parameters).UseThis(ThisParameter).Seal();
        }

        protected abstract Type GetDelegateType();
        protected abstract BlockExpression GetMethodBody();
    }
}
