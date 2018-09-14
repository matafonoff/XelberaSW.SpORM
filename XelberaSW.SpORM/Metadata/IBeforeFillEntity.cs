using System.Data;

namespace XelberaSW.SpORM.Metadata
{
    public interface IBeforeReadEntity
    {
        void PreProcessEntity(IDataRecord dataRecord);
    }
}
