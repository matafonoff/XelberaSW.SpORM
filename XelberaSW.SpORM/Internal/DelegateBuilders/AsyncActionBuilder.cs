using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace XelberaSW.SpORM.Internal.DelegateBuilders
{
    class AsyncActionBuilder : AsyncDelegateBuilder
    {
        private static readonly MethodInfo _readerWrapperMethodInfo = typeof(AsyncActionBuilder).GetMethod(nameof(ReaderWrapper), BindingFlags.Static | BindingFlags.NonPublic);

        public AsyncActionBuilder(MethodInfo caller) : base(caller)
        {
        }

        private static Func<Task<IDataReader>, Task> ReaderWrapper()
        {
            return async x =>
            {
                using (var reader = await x)
                { }
            };
        }

        #region Overrides of AsyncDelegateBuilder

        /// <inheritdoc />
        protected override MethodInfo GetReaderWrapper(MethodContext context)
        {
            return _readerWrapperMethodInfo;
        }

        #endregion
    }
}