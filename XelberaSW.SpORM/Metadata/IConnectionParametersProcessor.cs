using System.Data;

namespace XelberaSW.SpORM.Metadata
{
    public interface IConnectionParametersProcessor: IConnectionParameters
    {
        void Apply(IDbCommand command);
    }
}
