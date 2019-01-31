/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace XelberaSW.SpORM.Internal.DelegateBuilders
{
    class ActionBuilder : AsyncActionBuilder
    {
        private static readonly MethodInfo _configureAwait = typeof(Task).GetMethod(nameof(Task.ConfigureAwait));
        private static readonly MethodInfo _getAwaiter = typeof(ConfiguredTaskAwaitable).GetMethod(nameof(ConfiguredTaskAwaitable.GetAwaiter));
        private static readonly MethodInfo _getResult = typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter).GetMethod(nameof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter.GetResult));

        public ActionBuilder(MethodInfo caller) : base(caller)
        {
            throw new NotSupportedException("Not sopported yet");
        }

        /// <inheritdoc />
        protected override Type GetDelegateType()
        {
            var func = Type.GetType($"System.Action`{Parameters.Length}", true).MakeGenericType(Parameters.Select(x => x.Type).ToArray());
            return func;
        }

        /// <inheritdoc />
        protected override BlockExpression GetMethodBody()
        {
            var asyncBody = base.GetMethodBody();

            var syncBody = Expression.Block(new Expression[]
            {
                asyncBody,
                Expression.Call(Expression.Call(Expression.Call(asyncBody.Result, _configureAwait, Expression.Constant(false)), _getAwaiter), _getResult),
            });

            return syncBody;
        }
    }
}
