// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Maps;

namespace Game.Scripting.BaseScripts
{
	public class MapScript<T> : ScriptObject where T : Map
	{
		private MapRecord _mapEntry;

		public MapScript(string name, uint mapId) : base(name)
		{
			_mapEntry = CliDB.MapStorage.LookupByKey(mapId);

			if (_mapEntry == null)
				Log.outError(LogFilter.Scripts, "Invalid MapScript for {0}; no such map ID.", mapId);
		}

		// Gets the MapEntry structure associated with this script. Can return NULL.
		public MapRecord GetEntry()
		{
			return _mapEntry;
		}
	}
}