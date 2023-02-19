// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.SHADOWBURN)]
    public class spell_war_shadowburn_SpellScript : SpellScript, ISpellCalcCritChance
    {
        public void CalcCritChance(Unit victim, ref float chance)
        {
            if(GetCaster()?.TryGetAura(WarlockSpells.SHADOWBURN, out var shadowburn) == true)
				chance += shadowburn.GetEffect(2).GetBaseAmount();
        }
    }
}