using System.Collections.Generic;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class LocationRouteListResult
    {
        public RouteSummary Summary { get; set; }
        public ICollection<LocationInfo> Locations { get; set; }
    }

    public class RouteSummary
    {
        [ColumnName("FromLocationId")]
        public int LocationFrom { get; set; }
        [ColumnName("ToLocationId")]
        public int LocationTo { get; set; }
        public int? Distance { get; set; }
        public int? DayMin { get; set; }
        public int? DayMax { get; set; }
        public string Route { get; set; }
    }

    public class LocationInfo
    {
        public string LocationName { get; set; }
        public string NameFull { get; set; }
        public string CountryCode { get; set; }
        public string LocationType { get; set; }
        public string LocationAddress { get; set; }
        public int LocationId { get; set; }
        public int ParentId { get; set; }
        public string Kladr { get; set; }
        public int? RegionId { get; set; }
        public int TimeOffset { get; set; }
        public bool IsAvia { get; set; }
        public bool HasTerminal { get; set; }
    }

}
