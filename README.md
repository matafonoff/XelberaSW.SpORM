## RatekCC.SpORM

ORM for working with StoredProcedures

### Installation
No NuGet package available at nuget.org the moment.
You can use git submodules and add reference to csproj file directly.

### Usage

    public class MyDbContext : XelberaSW.SpORM.DbContext, ITestDbContext
    {
        private class DispatcherArguments
        {
            [SpParameter("@Action", Type = DbType.String, Size = 50)]
            public string ActionName { get; set; }
            [SpParameter("@Data", Type = DbType.Xml, Direction = ParameterDirection.InputOutput)]
            public object Data { get; set; }

            [SpErrorInformation]
            [SpParameter("@Message", Type = DbType.String, Size = 8000, Direction = ParameterDirection.InputOutput)]
            public string Message { get; set; }
        }

        public MyDbContext(DbContextParameters parameters)
            : base(parameters)
        { }

        public Task<IDataReader> Dispatcher(string actionName, object args, CancellationToken token)
        {
            return InvokeStoredProcedureAsync("spDispatcherProc", new DispatcherArguments
            {
                ActionName = actionName,
                Data = args
            }, token);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("HelloWorld")]
        [DbConnectionParameters(Timeout = 50000)]
        public Task<string> Login(string name, CancellationToken token)
        {
            return ExecStoredProcedure<Task<string>>(name, token);
        }
  }

