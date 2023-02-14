// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IPlayer
{
    public interface IPlayerOnCooldownStart : IScriptObject, IClassRescriction
    {
        void OnCooldownStart(Player player, SpellInfo spellInfo, uint itemId, uint categoryId, TimeSpan cooldown, ref DateTime cooldownEnd, ref DateTime categoryEnd, ref bool onHold);
    }
}
