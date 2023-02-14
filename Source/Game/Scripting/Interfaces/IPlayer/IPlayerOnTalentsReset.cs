// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player's talent points are reset (right before the reset is done);
    public interface IPlayerOnTalentsReset : IScriptObject
    {
        void OnTalentsReset(Player player, bool noCost);
    }
}