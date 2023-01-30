// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Maps;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.IMap;

namespace Game.Entities
{
    // hack to allow conditions to access what faction owns the map (these worldstates should not be set on these maps)
    internal class SplitByFactionMapScript : WorldMapScript, IMapOnCreate<Map>
    {
        public SplitByFactionMapScript(string name, uint mapId) : base(name, mapId)
        {
        }

        public void OnCreate(Map map)
        {
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceAlliance, map.GetInstanceId() == TeamId.Alliance ? 1 : 0, false, map);
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceHorde, map.GetInstanceId() == TeamId.Horde ? 1 : 0, false, map);
        }
    }
}