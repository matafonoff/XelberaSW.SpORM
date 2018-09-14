using System.Collections.Generic;

namespace XelberaSW.SpORM
{
    public interface IParameterConverter
    {
        object Convert(IDictionary<string, object> parameters);
    }
}
