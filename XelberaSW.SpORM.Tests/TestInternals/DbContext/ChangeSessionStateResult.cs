using System;
using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class ChangeSessionStateResult
    {
        public Guid SessionId { get; set; }

        [ColumnName("SessionStateID")]
        public UserStatus NewUserStatus { get; set; }

        [ColumnName("Visible")]
        public bool IsVisible { get; set; }
    }
}
