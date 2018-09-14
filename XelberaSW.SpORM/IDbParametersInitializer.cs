using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace XelberaSW.SpORM
{
    public interface IDbParametersInitializer
    {
        void Initialize(DbCommand cmd, List<IDbDataParameter> errorParams);
    }

}
