using System.Collections.Generic;

namespace Framework.Util
{
	public class VariableStore
	{
		private Dictionary<string, object> _variables = new();

		public T GetValue<T>(string key, T defaultValue)
		{
			lock (_variables)
			{
				if (_variables.TryGetValue(key, out var val) &&
				    typeof(T) == val.GetType())
					return (T)val;
			}

			return defaultValue;
		}

		public void Set<T>(string key, T objectVal)
		{
			lock (_variables)
			{
				_variables[key] = objectVal;
			}
		}
	}
}