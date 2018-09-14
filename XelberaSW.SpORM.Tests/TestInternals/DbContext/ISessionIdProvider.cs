using System;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public interface ISessionIdProvider
    {
        Guid Sessionid { get; }
    }
}