using System;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class DeliveryInfo
    {
        [ColumnName("DocumentID")]
        public int DocumentId { get; set; }
        public string Number { get; set; }
        [ColumnName("DeliveryTerminalID")]
        public int? DeliveryTerminalId { get; set; }
        [ColumnName("DeliveryAddressID")]
        public int? DeliveryAddressId { get; set; }
        public string DeliveryAddress { get; set; }
        public string DeliveryContactPerson { get; set; }
        public DateTimeOffset? DeliveryDateExecute { get; set; }
        public string DeliveryTimeLunch { get; set; }
        public string DeliveryTimeRunning { get; set; }
        [ColumnName("DeliveryUserID")]
        public int? DeliveryUserId { get; set; }
        public string DeliveryCargoDimentions { get; set; }
        public int DeliveryCargoUnloadWorkman { get; set; }
        [ColumnName("DeliveryCargoUnloadType")]
        public int? DeliveryCargoUnloadTypeId { get; set; }
        public decimal CostDelivery { get; set; }
        public decimal CostLoaderDelivery { get; set; }
        public string Note { get; set; }
        [ColumnName("StateID")]
        public int? StateId { get; set; }
        public string StateName { get; set; }
        public DateTimeOffset Date { get; set; }
        public string OperatorName { get; set; }
        [ColumnName("CargoInDocumentID")]
        public int? CargoInDocumentId { get; set; }
        public string CargoInNumber { get; set; }
        public DateTimeOffset CargoInDateAccept { get; set; }
        [ColumnName("LocationID")]
        public int? LocationId { get; set; }
        public string LocationFullName { get; set; }
        public string FullStreetName { get; set; }
        [ColumnName("LocationToID")]
        public int? LocationToId { get; set; }
        public int? PaymentMethod { get; set; }
    }
}