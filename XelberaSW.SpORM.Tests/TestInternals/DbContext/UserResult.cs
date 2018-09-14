using System;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class UserResult
    {
        public Guid SessionId { get; set; }

        [ColumnName("Phone")]
        public string PhoneNumber { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }

        [ColumnName("SessionStateCode")]
        public UserStatus Status { get; set; }
    }
}