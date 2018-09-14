using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace XelberaSW.SpORM.Internal.DelegateBuilders
{
    class FuncBuilder : AsyncFuncBuilder
    {
        private static readonly MethodInfo _configureAwait = typeof(Task<>).GetMethod(nameof(Task.ConfigureAwait));
        private static readonly MethodInfo _getAwaiter = typeof(ConfiguredTaskAwaitable<>).GetMethod(nameof(ConfiguredTaskAwaitable.GetAwaiter));
        private static readonly MethodInfo _getResult = typeof(ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter).GetMethod(nameof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter.GetResult));

        /// <inheritdoc />
        public FuncBuilder(MethodInfo caller)
            : base(caller)
        {
            throw new NotSupportedException("Not sopported yet");
        }

        protected override BlockExpression GetMethodBody()
        {
            var asyncBody = base.GetMethodBody();

            var configureAwait = _configureAwait.MakeGenericMethod(ReturnType);
            var getAwaiter = _getAwaiter.MakeGenericMethod(ReturnType);
            var getResult = _getResult.MakeGenericMethod(ReturnType);

            var syncBody = Expression.Block(new Expression[]
            {
                asyncBody,
                Expression.Call(Expression.Call(Expression.Call(asyncBody.Result, configureAwait, Expression.Constant(false)), getAwaiter), getResult),
            });

            return syncBody;
        }
    }
}