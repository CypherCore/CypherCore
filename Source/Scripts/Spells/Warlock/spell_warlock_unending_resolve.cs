using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    // 104773 - Unending Resolve
    [SpellScript(104773)]
    internal class spell_warlock_unending_resolve : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void PreventEffectIfCastingCircle(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster == null || caster.ToPlayer())
            {
                return;
            }
            Player pCaster = caster.ToPlayer();
            if (pCaster == null)
            {
                return;
            }

            if (pCaster.HasSpell(WarlockSpells.CASTING_CIRCLE))
            {
                PreventDefaultAction();
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real));
            AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
            AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 3, AuraType.MechanicImmunity, AuraEffectHandleModes.Real));
            AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 3, AuraType.MechanicImmunity, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
