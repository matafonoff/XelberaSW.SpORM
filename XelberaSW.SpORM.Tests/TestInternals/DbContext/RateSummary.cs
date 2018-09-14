namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class RateSummary
    {
        public int LocationFromId { get; set; }
        public int LocationToId { get; set; }
        public string LocationFrom { get; set; }
        public string LocationTo { get; set; }

        public int? RegionFromId { get; set; }
        public int? RegionToId { get; set; }
    }
}