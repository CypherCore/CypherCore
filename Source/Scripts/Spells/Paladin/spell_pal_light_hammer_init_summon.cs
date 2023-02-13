using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(122773)] // 122773 - Light's Hammer
    internal class spell_pal_light_hammer_init_summon : SpellScript, ISpellAfterCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.LightHammerCosmetic, PaladinSpells.LightHammerPeriodic);
        }

        public void AfterCast()
        {
            foreach (var summonedObject in GetSpell().GetExecuteLogEffect(SpellEffectName.Summon).GenericVictimTargets)
            {
                Unit hammer = Global.ObjAccessor.GetUnit(GetCaster(), summonedObject.Victim);

                if (hammer != null)
                {
                    hammer.CastSpell(hammer,
                                     PaladinSpells.LightHammerCosmetic,
                                     new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));

                    hammer.CastSpell(hammer,
                                     PaladinSpells.LightHammerPeriodic,
                                     new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell()));
                }
            }
        }
    }
}
