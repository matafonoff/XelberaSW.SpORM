using System.Data;

namespace XelberaSW.SpORM.Metadata
{
    public interface IAfterReadEntity
    {
        void PostProcessEntity(IDataRecord dataRecord);
    }
}