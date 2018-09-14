using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XelberaSW.SpORM.Tests.TestInternals.DbContext;

namespace XelberaSW.SpORM.Tests.TestInternals
{
    public class DbRelatedTestBase : DbRelatedTestBase<MyDbContext>
    {

    }

    public interface ITestDbContext
    {
        Task<UserResult> Login(string login, string password, string phone, CancellationToken token);
    }

    public class DbRelatedTestBase<TDbContext>
        where TDbContext : SpORM.DbContext, ITestDbContext
    {
        private IServiceProvider _services;

        protected virtual string ConnectionString => Constants.CONNECTION_STRING;

        [TestInitialize]
        public async Task Init()
        {
            var services = new ServiceCollection();

            services.AddLogging(x =>
            {
                x.AddConsole(c =>
                {
                    c.IncludeScopes = true;
                    c.DisableColors = false;
                });
            });


            services.UseDbContext<TDbContext>(x =>
            {
                x.ConnectionString = ConnectionString;
                x.UseDbConnection<SqlConnection>();
            });

            services.AddSingleton<ISessionIdProvider, SessionProvider>();

            _services = services.BuildServiceProvider();

            using (var ctx = _services.GetRequiredService<TDbContext>())
            {
                var result = await ctx.Login("matafonov.s", "123456", "4800", CancellationToken.None);

                _services.UseSessionId(result.SessionId);
            }
        }

        public T GetRequiredService<T>()
        {
            return _services.GetRequiredService<T>();
        }
    }
}
