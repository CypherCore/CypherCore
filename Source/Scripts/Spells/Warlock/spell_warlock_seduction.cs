using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 6358 - Seduction, 115268 - Mesmerize
    [SpellScript(new uint[] { 6358, 115268 })]
    public class spell_warlock_seduction : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            // Glyph of Demon Training
            Unit target = GetTarget();
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }
            Unit owner = caster.GetOwner();
            if (owner != null)
            {
                if (owner.HasAura(WarlockSpells.GLYPH_OF_DEMON_TRAINING))
                {
                    target.RemoveAurasByType(AuraType.PeriodicDamage);
                    target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
                }
            }

            // remove invisibility from Succubus on successful cast
            caster.RemoveAura(WarlockSpells.PET_LESSER_INVISIBILITY);
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real));
        }
    }
}
