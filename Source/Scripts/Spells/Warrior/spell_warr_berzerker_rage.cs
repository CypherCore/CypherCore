using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    //18499
    [SpellScript(18499)]
    public class spell_warr_berzerker_rage : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            caster.RemoveAurasDueToSpell(WarriorSpells.UNCHACKLED_FURY);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
