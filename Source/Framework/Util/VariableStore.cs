using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Util
{
    public class VariableStore
    {
        private Dictionary<string, object> _variables = new Dictionary<string, object>();

        public T GetValue<T>(string key, T defaultValue)
        {
            lock(_variables)
                if (_variables.TryGetValue(key, out var val) && typeof(T) == val.GetType())
                    return (T)val;

            return defaultValue;
        }

        public void Set<T>(string key, T objectVal)
        {
            lock (_variables)
                _variables[key] = objectVal;
        }
    }
}
