using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // Blood Reserve - 64568
internal class spell_gen_blood_reserve : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.BloodReserveHeal);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var caster = eventInfo.GetActionTarget();

		if (caster != null)
			if (caster.HealthBelowPct(35))
				return true;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var                caster = eventInfo.GetActionTarget();
		CastSpellExtraArgs args   = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount());
		caster.CastSpell(caster, GenericSpellIds.BloodReserveHeal, args);
		caster.RemoveAura(GenericSpellIds.BloodReserveAura);
	}
}