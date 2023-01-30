// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Maps;
using Game.PvP;

namespace Game.Scripting.Interfaces.IOutdoorPvP
{
    public interface IOutdoorPvPGetOutdoorPvP : IScriptObject
    {
        OutdoorPvP GetOutdoorPvP(Map map);
    }
}