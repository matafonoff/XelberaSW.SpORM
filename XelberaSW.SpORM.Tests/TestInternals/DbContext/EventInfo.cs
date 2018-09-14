using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class EventInfo
    {
        [ColumnName("EventID")]
        public int Id { get; set; }
        public string EventKey { get; set; }
        public EventType EventType { get; set; }
        public string EventName1 { get; set; }
        [ColumnName("EventName2")]
        public string Comment { get; set; }
        [ColumnName("EventDate")]
        public DateTimeOffset Date { get; set; }
        public string Address { get; set; }
        public int CargoStateId { get; set; }
        [ColumnName("DocumentStateID")]
        public int DocumentStateId { get; set; }
        public string TaskTypeName { get; set; }
        public bool IsTransit { get; set; }

        [IgnoreDataMember]
        public ICollection<EventInfoExt> EventInfoExts { get; set; }
    }
}