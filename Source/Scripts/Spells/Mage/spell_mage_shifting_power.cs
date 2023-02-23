// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage
{
    [SpellScript(MageSpells.ShiftingPowerDamageProc)]
    internal class spell_mage_shifting_power : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            var caster = GetCaster();

            if (caster != null && caster.TryGetAura(MageSpells.ShiftingPower, out var aura))
            {
                //creating a list of all spells in casters spell history
                var spellHistory = caster.GetSpellHistory();               
                // looping over all spells that have cooldowns
                foreach (var spell in spellHistory.SpellsOnCooldown)
                {
                    spellHistory.ModifyCooldown(spell, System.TimeSpan.FromMilliseconds(aura.GetSpellInfo().GetEffect(1).BasePoints));
                }
            }
        }
    }
}
