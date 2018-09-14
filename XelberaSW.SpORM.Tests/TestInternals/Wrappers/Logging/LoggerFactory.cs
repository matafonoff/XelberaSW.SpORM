//using System;
//using System.Collections.Generic;
//using System.Text;

//using Microsoft.Extensions.Logging;

//namespace RatekCC.SpORM.Tests.TestInternals.Wrappers.Logging
//{
//    class LoggerFactory : ILoggerFactory
//    {
//        /// <inheritdoc />
//        public void Dispose()
//        {

//        }

//        /// <inheritdoc />
//        public ILogger CreateLogger(string categoryName)
//        {
//            throw new NotImplementedException();
//        }

//        /// <inheritdoc />
//        public void AddProvider(ILoggerProvider provider)
//        {
//            throw new NotImplementedException();
//        }
//    }

//    class LoggerProvider : ILoggerProvider
//    {
//        /// <inheritdoc />
//        public void Dispose()
//        {

//        }

//        /// <inheritdoc />
//        public ILogger CreateLogger(string categoryName)
//        {
//           return new Logger<Type>();
//        }
//    }

//    class Logger : ILogger
//    {
//        class Scope : IDisposable
//        {
//            private object _state;

//            public Scope(object state)
//            {
//                _state = state;
//            }

//            public Action OnDispose { get; set; }

//            /// <inheritdoc />
//            public void Dispose()
//            {
//                OnDispose?.Invoke();
//            }
//        }

//        private readonly List<Scope> _scopes = new List<Scope>();

//        /// <inheritdoc />
//        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//        {
//            throw new NotImplementedException();
//        }

//        /// <inheritdoc />
//        public bool IsEnabled(LogLevel logLevel)
//        {
//            return true;
//        }

//        /// <inheritdoc />
//        public IDisposable BeginScope<TState>(TState state)
//        {
//            var scope = new Scope(state);
//            lock (_scopes)
//            {
//                _scopes.Add(scope);
//            }

//            scope.OnDispose = () =>
//            {
//                lock (_scopes)
//                {
//                    var idx = _scopes.IndexOf(scope);

//                    if (idx != -1)
//                    {
//                        _scopes.RemoveRange(idx, _scopes.Count - idx);
//                    }
//                }
//            };

//            return scope;
//        }
//    }

//}
