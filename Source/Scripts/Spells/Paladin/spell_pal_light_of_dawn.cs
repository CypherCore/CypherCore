// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    //Light of Dawn - 85222
    [SpellScript(85222)]
    public class spell_pal_light_of_dawn : SpellScript, ISpellOnHit, ISpellAfterCast
    {
        public override bool Validate(SpellInfo UnnamedParameter)
        {
            if (Global.SpellMgr.GetSpellInfo(PaladinSpells.LIGHT_OF_DAWN_TRIGGER, Difficulty.None) != null)
            {
                return false;
            }
            return true;
        }

        public void OnHit()
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(caster, PaladinSpells.LIGHT_OF_DAWN_TRIGGER, true);

                if (caster.HasAura(PaladinSpells.DIVINE_PURPOSE_HOLY_AURA_2))
                {
                    caster.RemoveAurasDueToSpell(PaladinSpells.DIVINE_PURPOSE_HOLY_AURA_2);
                }
            }
        }

        public void AfterCast()
        {
            Unit caster = GetCaster();

            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(PaladinSpells.AWAKENING))
            {
                if (RandomHelper.randChance(15))
                {
                    caster.CastSpell(null, PaladinSpells.AvengingWrath, true);

                    Aura avengingWrath = caster.GetAura(PaladinSpells.AvengingWrath);
                    if (avengingWrath != null)
                    {
                        avengingWrath.SetDuration(10000, true);
                    }
                }
            }
        }
    }
}
