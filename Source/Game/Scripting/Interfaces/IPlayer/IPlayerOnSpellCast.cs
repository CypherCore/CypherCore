// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IPlayer
{
    // Called in Spell.Cast.
    public interface IPlayerOnSpellCast : IScriptObject, IClassRescriction
    {
        void OnSpellCast(Player player, Spell spell, bool skipCheck);
    }
}