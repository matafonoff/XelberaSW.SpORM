using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XelberaSW.SpORM.Tests.TestInternals;
using XelberaSW.SpORM.Tests.TestInternals.DbContext;

namespace XelberaSW.SpORM.Tests
{
    [TestClass]
    public class TestLogin
    {
        private IServiceProvider _services;

        [TestInitialize]
        public void Init()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseDbContext<MyDbContext>(x =>
            {
                x.ConnectionString = Constants.CONNECTION_STRING;
                x.UseDbConnection<SqlConnection>();
            });

            services.AddSingleton<ISessionIdProvider, SessionProvider>();

            _services = services.BuildServiceProvider();
        }

        [TestMethod]
        public async Task CheckLogin()
        {
            using (var ctx = _services.GetRequiredService<MyDbContext>())
            {
                var result = await ctx.Login("matafonov.s", "123456", "4800", CancellationToken.None);

                Assert.IsNotNull(result);
                Assert.AreNotEqual(result.SessionId, Guid.Empty);

                _services.UseSessionId(result.SessionId);

                Trace.WriteLine("DONE! Session ID: " + result.SessionId);
            }
        }
    }
}