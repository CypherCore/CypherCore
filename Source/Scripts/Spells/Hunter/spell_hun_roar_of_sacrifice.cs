using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 53480 - Roar of Sacrifice
internal class spell_hun_roar_of_sacrifice : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.RoarOfSacrificeTriggered);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 1, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo == null ||
		    !Convert.ToBoolean((int)damageInfo.GetSchoolMask() & aurEff.GetMiscValue()))
			return false;

		if (!GetCaster())
			return false;

		return true;
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(eventInfo.GetDamageInfo().GetDamage(), aurEff.GetAmount()));
		eventInfo.GetActor().CastSpell(GetCaster(), HunterSpells.RoarOfSacrificeTriggered, args);
	}
}