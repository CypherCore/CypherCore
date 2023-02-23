// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Paladin
{
    // Justicar's Vengeance - 215661
    [SpellScript(215661)]
    public class spell_pal_justicars_vengeance : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target == null)
            {
                return;
            }

            if (target.HasAuraType(AuraType.ModStun) || target.HasAuraWithMechanic(1 << (int)Mechanics.Stun))
            {
                double damage = GetHitDamage();
                MathFunctions.AddPct(ref damage, 50);

                SetHitDamage(damage);
                SetEffectValue(damage);
            }

            if (caster.HasAura(PaladinSpells.DivinePurposeTriggerred))
            {
                caster.RemoveAura(PaladinSpells.DivinePurposeTriggerred);
            }

            if (caster.HasAura(PaladinSpells.FIST_OF_JUSTICE_RETRI))
            {
                if (caster.GetSpellHistory().HasCooldown(PaladinSpells.HammerOfJustice))
                {
                    caster.GetSpellHistory().ModifyCooldown(PaladinSpells.HammerOfJustice, TimeSpan.FromSeconds(-10 * Time.InMilliseconds));
                }
            }
        }
    }
}
