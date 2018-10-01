## XelberaSW.SpORM

ORM for working with StoredProcedures

### Installation
No NuGet package available at nuget.org at the moment.
You can use git submodules and add reference to csproj file directly.

### Usage

#### 1) Create context to access database

    public class MyDbContext : DbContext
    {
        // Dispatcher arguments
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

        // Dispatcher procedure **must** have following signature:
        // Returns: Task<IDataReader>
        // Arguments: 
        //  - string            - [MANDATORY] name of action to be executed
        //  - object            - [MANDATORY] any object with arguments for specified action
        //  - CancellationToken - [OPTIONAL] cancellation token for async operations
        private Task<IDataReader> Dispatcher(string actionName, object args, CancellationToken token)
        {
            // Invoke stored procedure by name with specified arguments
            // Notice that this method returns Task<IDataReader> but not typed entities
            return InvokeStoredProcedureAsync("spDispatcherProc", new DispatcherArguments
            {
                ActionName = actionName,
                Data = args
            }, token);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // Mandatory at the moment
        [StoredProcedure("HelloWorld")]             // Name of action
        [DbConnectionParameters(Timeout = 50000)]   // Some database connection parameters
        public Task<string> SayHello(string name, CancellationToken token)
        {
            // Calling protected method that will prepare compiled lambda expression 
            // for reading data from database, map everything to entity of specified type
            // and replace current method by call to lambda expression.
            // Supported output types:
            //  - simple types (like string, int, double and etc) and 
            //    enums types (value from database could be a string or number) and
            //    Lists of objects of these types
            //  - class with properties of types from previous point (simple entities) and
            //    Lists of objects of these types
            //  - classes with properties of types from previos point (complex entites) and
            //    Lists of objects of these types
            return ExecStoredProcedure<Task<string>>(name, token);
        }
    }
  
  
#### 2) Add it to IoC (somewhere in Startup.cs:ConfigureServices)

    services.UseDbContext<MyDbContext>(x =>
    {
        x.ConnectionString = ConnectionString;  // connection string to be used
        x.UseDbConnection<SqlConnection>();     // Use specified IDbConnection implementation
    });

#### 3) Use it

    class MyService 
    {
        private readonly MyDbContext _ctx;
        public MyService(MyDbContext ctx)
        {
            _ctx = ctx;
        }
        
        public async Task<string> SayHelloTo(string name) 
        {
            var result = await _ctx.SayHello(name);
            
            return result;
        }
    }
    
    
Full documentation and nuget package are coming soon! Please keep in touch!
