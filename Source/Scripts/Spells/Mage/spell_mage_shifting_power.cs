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
                System.TimeSpan mod = new System.TimeSpan(0, 0, (aura.GetSpellInfo().GetEffect(1).BasePoints / 1000));
                // looping over all spells that have cooldowns
                foreach (var spell in spellHistory.SpellsOnCooldown)
                {
                    spellHistory.ModifyCooldown(spell, mod);
                }
            }
        }
    }
}
