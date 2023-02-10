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
            if (Global.SpellMgr.GetSpellInfo(PaladinSpells.SPELL_PALADIN_LIGHT_OF_DAWN_TRIGGER, Difficulty.None) != null)
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
                caster.CastSpell(caster, PaladinSpells.SPELL_PALADIN_LIGHT_OF_DAWN_TRIGGER, true);

                if (caster.HasAura(PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_HOLY_AURA_2))
                {
                    caster.RemoveAurasDueToSpell(PaladinSpells.SPELL_PALADIN_DIVINE_PURPOSE_HOLY_AURA_2);
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

            if (caster.HasAura(PaladinSpells.SPELL_PALADIN_AWAKENING))
            {
                if (RandomHelper.randChance(15))
                {
                    caster.CastSpell(null, PaladinSpells.SPELL_PALADIN_AVENGING_WRATH, true);

                    Aura avengingWrath = caster.GetAura(PaladinSpells.SPELL_PALADIN_AVENGING_WRATH);
                    if (avengingWrath != null)
                    {
                        avengingWrath.SetDuration(10000, true);
                    }
                }
            }
        }
    }
}
