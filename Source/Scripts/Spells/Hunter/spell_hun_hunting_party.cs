using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 212658 - Hunting Party
internal class spell_hun_hunting_party : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.Exhilaration, HunterSpells.ExhilarationPet);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().GetSpellHistory().ModifyCooldown(HunterSpells.Exhilaration, -TimeSpan.FromSeconds(aurEff.GetAmount()));
		GetTarget().GetSpellHistory().ModifyCooldown(HunterSpells.ExhilarationPet, -TimeSpan.FromSeconds(aurEff.GetAmount()));
	}
}