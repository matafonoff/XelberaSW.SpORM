using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using XelberaSW.SpORM.Internal.Readers;

namespace XelberaSW.SpORM.Internal.DelegateBuilders
{
    class AsyncFuncBuilder : AsyncDelegateBuilder
    {
        private static readonly MethodInfo _readerWrapperMethodInfo = typeof(AsyncFuncBuilder).GetMethod(nameof(ReaderWrapper), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo _simpleResultReaderMethodInfo = typeof(AsyncFuncBuilder).GetMethod(nameof(SimpleResultReaderWrapper), BindingFlags.Static | BindingFlags.NonPublic);


        public AsyncFuncBuilder(MethodInfo caller) : base(caller)
        { }

        private static Func<Task<IDataReader>, Task<T>> SimpleResultReaderWrapper<T>()
        {
            var obj = Expression.Parameter(typeof(object), "obj");

            var lambda = Expression.Lambda<Func<object, T>>(ValueConverter.Default.GetConvertValueExpression(obj, typeof(T)), obj);
            var converter = lambda.Compile();

            return async x =>
            {
                T result = default(T);
                using (var reader = await x)
                {
                    if (reader.Read() && reader.FieldCount > 0)
                    {
                        result = converter(reader.GetValue(0));
                    }
                }

                return result;
            };
        }

        private static Func<Task<IDataReader>, Task<T>> ReaderWrapper<T>()
            where T : class, new()
        {
            var modelReader = new ComplexModelReader<T>();
            var metaReader = new MetadataReader<T>();

            return async x =>
            {
                T result;
                using (var reader = await x)
                {
                    result = modelReader.Read(reader);

                    if (reader is DbReaderWrapper wrapper)
                    {
                        var payload = wrapper.Output;
                        if (payload != null)
                        {
                            metaReader.GetReader()(payload, result);
                        }
                    }
                }

                return result;
            };
        }

        #region Overrides of AsyncDelegateBuilder

        /// <inheritdoc />
        protected override MethodInfo GetReaderWrapper(MethodContext methodContext)
        {
            var reader =
                (methodContext.ReturnType.IsSimpleType() ?
                     _simpleResultReaderMethodInfo :
                     _readerWrapperMethodInfo)
                .MakeGenericMethod(methodContext.ReturnType);

            return reader;
        }

        #endregion
    }
}
