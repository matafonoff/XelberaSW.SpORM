using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using XelberaSW.SpORM.Metadata;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedMember.Local

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class MyDbContext : XelberaSW.SpORM.DbContext, ITestDbContext
    {
        private readonly ISessionIdProvider _sessionIdProvider;

        private class DispatcherArguments
        {
            [SpParameter("@Action", Type = DbType.String, Size = 50)]
            public string ActionName { get; set; }
            [SpParameter("@SessionId", Type = DbType.Guid)]
            public Guid? SessionId { get; set; }
            [SpParameter("@Data", Type = DbType.Xml, Direction = ParameterDirection.InputOutput)]
            public object Data { get; set; }

            [SpErrorInformation]
            [SpParameter("@Message", Type = DbType.String, Size = 8000, Direction = ParameterDirection.InputOutput)]
            public string Message { get; set; }
        }

        public MyDbContext(DbContextParameters parameters, ISessionIdProvider sessionIdProvider)
            : base(parameters)
        {
            _sessionIdProvider = sessionIdProvider;
        }

        public Task<IDataReader> NoSessionInfoDispatcher(string actionName, object args, CancellationToken token)
        {
            return InvokeStoredProcedureAsync("spCallCenter", new DispatcherArguments
            {
                ActionName = actionName,
                SessionId = null,
                Data = args
            }, token);
        }

        public Task<IDataReader> Dispatcher(string actionName, object args, CancellationToken token)
        {
            return InvokeStoredProcedureAsync("spCallCenter", new DispatcherArguments
            {
                ActionName = actionName,
                SessionId = _sessionIdProvider.Sessionid,
                Data = args
            }, token);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("Login")]
        [DbConnectionParameters(Timeout = 50000)]
        [UseDispatcher(nameof(NoSessionInfoDispatcher))]
        public Task<UserResult> Login(string login, string password, string phone, CancellationToken token)
        {
            return ExecStoredProcedure<Task<UserResult>>(login, password, phone, token);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("ChangeSessionState")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<ChangeSessionStateResult> ChangeSessionStateAsync([Parameter("SessionStateID", Type = typeof(int))]UserStatus newUserStatus)
        {
            return ExecStoredProcedure<Task<ChangeSessionStateResult>>((int)newUserStatus);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("AirTerminal")]
        [UseDispatcher(nameof(NoSessionInfoDispatcher))]
        public Task<AirTerminalResult> AirTerminal([Parameter("LocationID")]int locationId)
        {
            return ExecStoredProcedure<Task<AirTerminalResult>>(locationId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("LocationRouteList")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<LocationRouteListResult> LocationRouteList([Parameter("LocationFromID")]int locationFrom,
                                                               [Parameter("LocationToID")]int locationTo)
        {
            return ExecStoredProcedure<Task<LocationRouteListResult>>(locationFrom, locationTo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("Tariff")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<RateResult> GetTariff([Parameter("LocationFromID")]int locationFrom,
                                          [Parameter("LocationToID")]int locationTo)
        {
            return ExecStoredProcedure<Task<RateResult>>(locationFrom, locationTo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("StreetList")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<List<Street>> GetStreetListAsync([Parameter("LocationID")]int locationId)
        {
            return ExecStoredProcedure<Task<List<Street>>>(locationId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("AirTerminal", ParameterConverter = typeof(MyCustomConverter))]
        [UseDispatcher(nameof(NoSessionInfoDispatcher))]
        public Task<AirTerminalResult> CustomConverterTest(string s)
        {
            return ExecStoredProcedure<Task<AirTerminalResult>>(s);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("DocumentList")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<DataSourceResult<DocumentDetailedInfo>> GetDocumentsByAsync([Parameter("Request")]string requestParams, [Parameter("ContractorTemplate")]string contractorTemplate, [Parameter("PhoneTemplate")]string phoneTemplate)
        {
            return ExecStoredProcedure<Task<DataSourceResult<DocumentDetailedInfo>>>(requestParams, contractorTemplate, phoneTemplate);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("TripData")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<TripDetailedInfo> GetTripDetailedInfoAsync([Parameter("DocumentID")]int documentId)
        {
            return ExecStoredProcedure<Task<TripDetailedInfo>>(documentId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("CreateDelivery")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<DeliveryInfo> CreateDeliveryAsync(DeliveryInfo deliveryInfo)
        {
            return ExecStoredProcedure<Task<DeliveryInfo>>(deliveryInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("SendEmailMessage")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task SendEmailMessageAsync(EmailMessageRequest request)
        {
            return ExecStoredProcedure<Task>(request);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("CreateComment")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<EventInfoResult> CreateCommentAsync(CommentInfo commentInfo)
        {
            return ExecStoredProcedure<Task<EventInfoResult>>(commentInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("GetEmailMessageBody")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<string> GetEmailMessageBodyAsync()
        {
            return ExecStoredProcedure<Task<string>>();
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("GetEmailMessageBody")]
        [UseDispatcher(nameof(Dispatcher))]
        public string GetEmailMessageBody()
        {
            return ExecStoredProcedure<string>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("ContractorPaymentList")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<InvoiceSearchResult> ContractorPaymentListAsync(InvoiceSearchRequest invoiceSearchRequest)
        {
            return ExecStoredProcedure<Task<InvoiceSearchResult>>(invoiceSearchRequest);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [StoredProcedure("UserList")]
        [UseDispatcher(nameof(Dispatcher))]
        public Task<List<Employer>> GetUserListAsync(UserListQueryParameters parameters)
        {
            return ExecStoredProcedure<Task<List<Employer>>>(parameters);
        }
    }


    public class UserListQueryParameters
    {
        public string UserGroup { get; set; }

        [ColumnName("LocationID")]
        public int? LocationId { get; set; }
        [ColumnName("IsOperatorCC")]
        public bool? IsOperator { get; set; }

        private static object GetValueOf_IsOperator(bool? initialValue)
        {
            if (!initialValue.HasValue)
            {
                return null;
            }

            return initialValue.Value ? 1 : 0;
        }
    }

    public class Employer
    {
        public int UserId { get; set; }
        [ColumnName("UserName")]
        public string Name { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
    }
}
