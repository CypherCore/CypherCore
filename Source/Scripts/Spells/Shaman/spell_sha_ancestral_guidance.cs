using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 108281 - Ancestral Guidance
internal class spell_sha_ancestral_guidance : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.AncestralGuidanceHeal);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetHealInfo().GetSpellInfo().Id == ShamanSpells.AncestralGuidanceHeal)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		int bp0 = MathFunctions.CalculatePct((int)eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount());

		if (bp0 != 0)
		{
			CastSpellExtraArgs args = new(aurEff);
			args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
			eventInfo.GetActor().CastSpell(eventInfo.GetActor(), ShamanSpells.AncestralGuidanceHeal, args);
		}
	}
}