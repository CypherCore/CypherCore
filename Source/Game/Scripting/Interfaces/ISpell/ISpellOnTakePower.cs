// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Spells;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellOnTakePower : ISpellScript
    {
        public void TakePower(SpellPowerCost cost);
    }
}