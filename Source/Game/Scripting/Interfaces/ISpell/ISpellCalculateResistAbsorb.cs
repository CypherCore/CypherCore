// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellCalculateResistAbsorb : ISpellScript
    {
        void CalculateResistAbsorb(DamageInfo damageInfo, ref double resistAmount, ref double absorbAmount);
    }
}