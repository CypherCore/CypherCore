using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{ 
    // 267344 - Art of War
    [SpellScript(267344)]
    internal class spell_pal_art_of_war : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.ArtOfWarTriggered, PaladinSpells.BLADE_OF_JUSTICE);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
        }

        private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            return RandomHelper.randChance(aurEff.GetAmount());
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            GetTarget().GetSpellHistory().ResetCooldown(PaladinSpells.BLADE_OF_JUSTICE, true);
            GetTarget().CastSpell(GetTarget(), PaladinSpells.ArtOfWarTriggered, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
        }
    }
}
