// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Maps;

namespace Game.Scripting.Interfaces.IWorldState
{
    public interface IWorldStateOnValueChange : IScriptObject
    {
        void OnValueChange(int worldStateId, int oldValue, int newValue, Map map);
    }
}