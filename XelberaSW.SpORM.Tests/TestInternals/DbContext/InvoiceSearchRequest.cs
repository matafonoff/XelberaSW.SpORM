using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class InvoiceSearchRequest
    {
        [ColumnName("ContractorID")]
        public int ContractorId { get; set; }
        public int? Filter { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public string SortField { get; set; }
        public string SortDirection { get; set; }
    }

    public class InvoiceSearchRequest2 : InvoiceSearchRequestBase
    {
        public int? PageNumber { get; set; }
        public string SortField { get; set; }
        public string SortDirection { get; set; }
    }

    public class InvoiceSearchRequestBase
    {
        [ColumnName("ContractorID")]
        public int ContractorId { get; set; }
        public int? Filter { get; set; }
        public int? PageSize { get; set; }
    }
}