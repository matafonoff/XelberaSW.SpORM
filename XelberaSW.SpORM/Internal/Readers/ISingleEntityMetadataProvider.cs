using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace XelberaSW.SpORM.Internal.Readers
{
    interface ISingleEntityMetadataProvider
    {
        Dictionary<string, ReadOnlyCollection<PropertyInfo>> Properties { get; }
    }
}