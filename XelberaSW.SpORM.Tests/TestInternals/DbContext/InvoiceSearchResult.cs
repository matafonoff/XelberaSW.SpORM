using System.Collections.Generic;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class InvoiceSearchResult
    {
        [DataTableIndex(0)]
        public List<InvoiceDetailsWithCount> Invoices { get; set; }
    }
}