// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Maps;
using Game.PvP;

namespace Game.Scripting.Interfaces.IOutdoorPvP
{
    public interface IOutdoorPvPGetOutdoorPvP : IScriptObject
    {
        OutdoorPvP GetOutdoorPvP(Map map);
    }
}