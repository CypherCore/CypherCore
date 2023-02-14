// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Maps;

namespace Game.Scripting.BaseScripts
{
    public class WorldMapScript : MapScript<Map>
    {
        public WorldMapScript(string name, uint mapId) : base(name, mapId)
        {
            if (GetEntry() != null &&
                !GetEntry().IsWorldMap())
                Log.outError(LogFilter.Scripts, "WorldMapScript for map {0} is invalid.", mapId);

            Global.ScriptMgr.AddScript(this);
        }
    }
}