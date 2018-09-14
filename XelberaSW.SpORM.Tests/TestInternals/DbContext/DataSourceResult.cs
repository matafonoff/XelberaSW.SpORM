using System.Collections.Generic;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class DataSourceResult<T>
    {
        public ICollection<T> Data { get; set; }
        [MetadataXPath("/Output/@Total")]
        public int Total { get; set; }
        public object Errors { get; set; }
    }
}