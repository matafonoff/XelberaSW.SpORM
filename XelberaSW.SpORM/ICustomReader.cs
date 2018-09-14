using System.Data;

namespace XelberaSW.SpORM
{
    public interface ICustomReader
    {
        void Read(IDataRecord record);
    }
}
