using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class InvoiceDetailsWithCount : InvoiceDetails
    {
        [ColumnName("TotalCount")]
        public int InvoiceCount { get; set; }
    }
}