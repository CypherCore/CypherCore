// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Maps;

namespace Game.Scripting.BaseScripts
{
    public class WorldMapScript : MapScript<Map>
    {
        public WorldMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null && !GetEntry().IsWorldMap())
                Log.outError(LogFilter.Scripts, "WorldMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }
    }

}
