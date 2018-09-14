using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class RateEntry
    {
        [ColumnName("Tariff")]
        public double Rate { get; set; }
        public string Name { get; set; }
        public double From { get; set; }
        public double To { get; set; }
        public int OrderId { get; set; }
        [ColumnName("TariffType")]
        public RateType RateType { get; set; }

    }
}