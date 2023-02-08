using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{

    // 48181 - Haunt
    [SpellScript(48181)]
    public class aura_warl_haunt : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null || GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
            {
                return;
            }

            caster.GetSpellHistory().ResetCooldown(WarlockSpells.HAUNT, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.ModSchoolMaskDamageFromCaster, AuraEffectHandleModes.RealOrReapplyMask));
        }
    }
}
