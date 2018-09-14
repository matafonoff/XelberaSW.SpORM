using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XelberaSW.SpORM.Metadata;
using XelberaSW.SpORM.Tests.TestInternals;
using XelberaSW.SpORM.Tests.TestInternals.DbContext;

namespace XelberaSW.SpORM.Tests
{
    public class Data
    {
        public DateTimeOffset Value { get; set; }
    }

    [TestClass]
    public class DateTimeOffsetTest : DbRelatedTestBase<DateTimeOffsetTest.SimpleSessionProviderContext>
    {
        /// <inheritdoc />
        protected override string ConnectionString => "Data Source=git.dmn-ratek.local; Initial Catalog=sporm_testdb; persist security info=True;user id=Webdeveloper;password=web;";

        public class SimpleSessionProviderContext : DbContext, ITestDbContext
        {
            /// <inheritdoc />
            public SimpleSessionProviderContext(DbContextParameters parameters) : base(parameters)
            { }

            [StoredProcedure("spDateTimeOffsetTest")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public Task<Data> GetValue()
            {
                return ExecStoredProcedure<Task<Data>>();
            }

            /// <inheritdoc />
            public Task<UserResult> Login(string login, string password, string phone, CancellationToken token)
            {
                return Task.FromResult(new UserResult
                {
                    SessionId = Guid.NewGuid(),
                    PhoneNumber = phone,
                    RoleName = "",
                    StartDate = DateTimeOffset.Now,
                    UserName = login,
                    Status = UserStatus.Ready
                });
            }
        }

        [TestMethod]
        public async Task CheckDateTimeOffsetConversion()
        {
            using (var ctx = GetRequiredService<SimpleSessionProviderContext>())
            {
                var value = await ctx.GetValue();


            }
        }
    }
}
