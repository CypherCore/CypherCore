// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellEnergizedBySpell : ISpellScript
    {
        void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType);
    }
}
