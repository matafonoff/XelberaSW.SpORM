using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XelberaSW.SpORM.Tests.TestInternals;
using XelberaSW.SpORM.Tests.TestInternals.DbContext;

namespace XelberaSW.SpORM.Tests
{
    [TestClass]
    public class ParallelExecTests : DbRelatedTestBase
    {
        [TestMethod]
        public void DoAsyncCalls()
        {
            using (var ctx = GetRequiredService<MyDbContext>())
            {
                var runEvent = new ManualResetEvent(false);
                var threads = new List<Thread>();
                for (int i = 0; i < 100; i++)
                {
                    var t = new Thread(() => DoAsyncCall(ctx, runEvent))
                    {
                        IsBackground = true
                    };
                    t.Start();

                    threads.Add(t);
                }

                runEvent.Set();

                while (threads.Any())
                {
                    threads.RemoveAll(x => x.Join(0));
                }
            }
        }

        private void DoAsyncCall(MyDbContext ctx, ManualResetEvent runEvent)
        {
            var onFinish = new ManualResetEvent(false);

            runEvent.WaitOne();

            ctx.ChangeSessionStateAsync(UserStatus.Ready).GetAwaiter().OnCompleted(() =>
            {
                Trace.WriteLine("DONE");
                onFinish.Set();
            });

            onFinish.WaitOne();
        }
    }
}