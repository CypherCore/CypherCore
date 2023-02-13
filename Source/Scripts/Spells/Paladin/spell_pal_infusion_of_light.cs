using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(54149)] // 54149 - Infusion of Light
    internal class spell_pal_infusion_of_light : AuraScript, IHasAuraEffects
    {
        private static readonly FlagArray128 HolyLightSpellClassMask = new(0, 0, 0x400);
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.InfusionOfLightEnergize);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckFlashOfLightProc, 0, AuraType.AddPctModifier));
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckFlashOfLightProc, 2, AuraType.AddFlatModifier));

            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckHolyLightProc, 1, AuraType.Dummy));
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckFlashOfLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetProcSpell() && eventInfo.GetProcSpell().m_appliedMods.Contains(GetAura());
        }

        private bool CheckHolyLightProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Paladin, HolyLightSpellClassMask);
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            eventInfo.GetActor()
                     .CastSpell(eventInfo.GetActor(),
                                PaladinSpells.InfusionOfLightEnergize,
                                new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(eventInfo.GetProcSpell()));
        }
    }
}
