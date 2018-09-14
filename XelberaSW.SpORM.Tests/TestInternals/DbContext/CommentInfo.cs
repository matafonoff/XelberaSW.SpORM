using XelberaSW.SpORM.Metadata;

namespace XelberaSW.SpORM.Tests.TestInternals.DbContext
{
    public class CommentInfo
    {
        [ColumnName("DocumentID")]
        public int DocumentId { get; set; }
        [ColumnName("CargoStateID")]
        public int? CargoStateId { get; set; }
        [ColumnName("TerminalID")]
        public int? TerminalId { get; set; }
        public string Note { get; set; }
    }
}