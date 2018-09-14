using System;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class TripDetailedInfo
    {
        public int DocumentId { get; set; }
        public string Number { get; set; }
        public string Route { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Note { get; set; }
        public string TransportNote { get; set; }
        public DateTimeOffset? TripArrivalDate { get; set; }
        public DateTimeOffset? MaxArrivalDate { get; set; }
        public string TripTransportNumber { get; set; }
        public string TripTransportNumberExt { get; set; }
        public string TripTransportBrand { get; set; }
        public int LocationFromId { get; set; }
        public int LocationToId { get; set; }
        public string TripDriverName { get; set; }
        public string TripDriverIdentityCard { get; set; }
        public int TripTransportCapacity { get; set; }
        public string TripTransportTonnage { get; set; }
        public decimal TripCost { get; set; }

        [ColumnName("DocumentId")]
        public string DocumentId2 { get; set; }

    }
}