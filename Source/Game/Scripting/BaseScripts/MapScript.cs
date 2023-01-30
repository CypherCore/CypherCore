// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.DataStorage;
using Game.Maps;
using System.Collections.Generic;

namespace Game.Scripting.BaseScripts
{
    public class MapScript<T> : ScriptObject where T : Map
    {
        public MapScript(string name, uint mapId) : base(name)
        {
            _mapEntry = CliDB.MapStorage.LookupByKey(mapId);

            if (_mapEntry == null)
                Log.outError(LogFilter.Scripts, "Invalid MapScript for {0}; no such map ID.", mapId);
        }

        // Gets the MapEntry structure associated with this script. Can return NULL.
        public MapRecord GetEntry() { return _mapEntry; }

        MapRecord _mapEntry;
    }

}
