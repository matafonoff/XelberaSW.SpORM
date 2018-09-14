using System;
using System.Collections.Generic;

namespace XelberaSW.SpORM.Utilities
{
    static class SyncRoot
    {
        private static readonly Dictionary<string, object> _syncRoots = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public static object Get(string key)
        {
            var obj = _syncRoots.GetOrAddValueSafe(key, _ => new object());
            //Trace.WriteLine($"[{key}] sync object id: {obj.GetHashCode()}", nameof(SyncRoot));
            return obj;
        }

    }
}
