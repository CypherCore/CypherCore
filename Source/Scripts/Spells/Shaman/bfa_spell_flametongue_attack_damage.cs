using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
    // Flametongue Attack - 10444
    [SpellScript(10444)]
    public class bfa_spell_flametongue_attack_damage : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            Aura flamet = caster.GetAura(ShamanSpells.SPELL_FLAMETONGUE_AURA);
            if (flamet != null)
            {
                SetHitDamage((int)(caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.2f));
            }
        }
    }
}
