// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Maps;

namespace Game.Scripting.BaseScripts
{
    public class MapScript<T> : ScriptObject where T : Map
    {
        private readonly MapRecord _mapEntry;

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