using System;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DbConnectionParametersAttribute:Attribute, IConnectionParameters
    {
        public int Timeout
        {
            get;
            set;
        }
    }
}
