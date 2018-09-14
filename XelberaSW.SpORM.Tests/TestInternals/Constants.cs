using System;
using Microsoft.Extensions.DependencyInjection;
using XelberaSW.SpORM.Tests.TestInternals.DbContext;

namespace XelberaSW.SpORM.Tests.TestInternals
{
    static class Constants
    {
        internal const string CONNECTION_STRING = "Data Source=10.54.11.4; Initial Catalog=Callcenter_test; user id=Webdeveloper;password=web;";

        public static void UseSessionId(this IServiceProvider services ,Guid sessionId)
        {
            ((SessionProvider)services.GetRequiredService<ISessionIdProvider>()).Sessionid = sessionId;
        }
    }

    class SessionProvider : ISessionIdProvider
    {

        /// <inheritdoc />
        public Guid Sessionid
        {
            get;
            set;
        }
    }
}
