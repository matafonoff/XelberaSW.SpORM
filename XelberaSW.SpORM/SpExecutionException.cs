using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace XelberaSW.SpORM
{
    [Serializable]
    public class SpExecutionException : AggregateException
    {
        public SpExecutionException()
        {
        }

        public SpExecutionException(IEnumerable<Exception> innerExceptions) : base(innerExceptions)
        {
        }

        public SpExecutionException(params Exception[] innerExceptions) : base(innerExceptions)
        {
        }

        public SpExecutionException(string message) : base(message)
        {
        }

        public SpExecutionException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions)
        {
        }

        public SpExecutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SpExecutionException(string message, params Exception[] innerExceptions) : base(message, innerExceptions)
        {
        }

        protected SpExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
