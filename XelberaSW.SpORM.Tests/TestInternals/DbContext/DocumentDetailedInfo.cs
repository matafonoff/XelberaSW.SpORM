using System;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class DocumentDetailedInfo
    {
        public Guid SessionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int TaskId { get; set; }
        public int DocumentId { get; set; }
        public int TypeId { get; set; }
        public int StateId { get; set; }
        [ColumnName("Date")]
        public DateTimeOffset StateStartDate { get; set; }
        [ColumnName("Note")]
        public string Comment { get; set; }
        public string Number { get; set; }
        public string LocationFrom { get; set; }
        public string LocationTo { get; set; }
        public string SenderContractorName { get; set; }
        public string RecipientContractorName { get; set; }
        public string PaymentContractorName { get; set; }

        public decimal QuantityPackage { get; set; }
        public double Weight { get; set; }
        public double Volume { get; set; }
        public decimal CostTotal { get; set; }
    }
}