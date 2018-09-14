using System;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class InvoiceDetails
    {
        [ColumnName("InvoiceID")]
        public int InvoiceId { get; set; }
        [ColumnName("TTNDocumentID")]
        public int TtnDocumentId { get; set; }
        public string InvoiceNumber { get; set; }
        [ColumnName("TTNNumber")]
        public string TtnNumber { get; set; }
        public DateTimeOffset InvoiceDate { get; set; }
        public decimal BilledForPayment { get; set; }
        public decimal CostPayment { get; set; }
        public DateTimeOffset? TransferDate { get; set; }
        public string LocationFrom { get; set; }
        public string LocationTo { get; set; }
        public int SenderContractorId { get; set; }
        public int RecipientContractorId { get; set; }
        [ColumnName("SenderContractorName")]
        public string Sender { get; set; }
        [ColumnName("RecipientContractorName")]
        public string Recipient { get; set; }
    }
}