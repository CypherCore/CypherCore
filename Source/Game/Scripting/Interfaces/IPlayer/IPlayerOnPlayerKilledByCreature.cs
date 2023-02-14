// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called when a player is killed by a creature
    public interface IPlayerOnPlayerKilledByCreature : IScriptObject
    {
        void OnPlayerKilledByCreature(Creature killer, Player killed);
    }
}