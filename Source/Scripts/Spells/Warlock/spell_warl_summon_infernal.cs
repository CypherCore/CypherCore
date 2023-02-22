// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [SpellScript(1122)]
    internal class spell_warl_summon_infernal : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            var caster = GetCaster();

            if(caster != null && caster.TryGetAura(WarlockSpells.CRASHING_CHAOS, out var aura))
                for (int i = 0; i < aura.GetEffect(0).m_baseAmount; i++) 
                    caster.AddAura(WarlockSpells.CRASHING_CHAOS_AURA, caster);
        }
    }
}