using System.Collections.Generic;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class RateResult
    {
        public RateSummary Summary { get; set; }
        public ICollection<RateEntry> RateEntries { get; set; }
        public ICollection<MovementRateEntity> MovementRateEntities { get; set; }
        public BonusRateEntity Bonus { get; set; }
    }
}
