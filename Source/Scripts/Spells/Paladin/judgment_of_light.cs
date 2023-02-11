﻿using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IUnit;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    //183778
    [Script]
    public class judgment_of_light : ScriptObjectAutoAdd, IUnitOnDamage
    {
        public judgment_of_light() : base("judgment_of_light")
        {
        }

        public void OnDamage(Unit caster, Unit target, ref uint damage)
        {
            Player player = caster.ToPlayer();
            if (player != null)
            {
                if (player.GetClass() != Class.Paladin)
                {
                    return;
                }
            }

            if (caster == null || target == null)
            {
                return;
            }

            if (caster.HasAura(PaladinSpells.SPELL_PALADIN_JUDGMENT_OF_LIGHT) && target.HasAura(PaladinSpells.SPELL_PALADIN_JUDGMENT_OF_LIGHT_TARGET_DEBUFF))
            {
                if (caster.IsWithinMeleeRange(target))
                {
                    caster.CastSpell(null, PaladinSpells.SPELL_PALADIN_JUDGMENT_OF_LIGHT_HEAL, true);
                    target.RemoveAura(PaladinSpells.SPELL_PALADIN_JUDGMENT_OF_LIGHT_TARGET_DEBUFF, ObjectGuid.Empty, 0, AuraRemoveMode.EnemySpell);
                }
            }
        }
    }
}