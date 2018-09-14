using System.Collections.Generic;

namespace XelberaSW.SpORM.Tests.TestInternals
{
    class MyCustomConverter:IParameterConverter
    {
        /// <inheritdoc />
        public object Convert(IDictionary<string, object> parameters)
        {
            return new
            {
                LocationID = System.Convert.ToInt32(parameters["s"])
            };
        }
    }
}
