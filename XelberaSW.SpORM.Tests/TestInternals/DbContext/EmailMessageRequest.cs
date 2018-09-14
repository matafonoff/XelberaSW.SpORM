using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class EmailMessageRequest
    {
        [ColumnName("DocumentID")]
        public int DocumentId { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}