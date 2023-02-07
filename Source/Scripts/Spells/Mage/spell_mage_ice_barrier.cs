using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 11426 - Ice Barrier
internal class spell_mage_ice_barrier : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellEntry)
	{
		return ValidateSpellInfo(MageSpells.Chilled);
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new EffectProcHandler(HandleProc, 0, AuraType.SchoolAbsorb, AuraScriptHookType.EffectProc));
	}

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
	{
		canBeRecalculated = false;
		Unit caster = GetCaster();

		if (caster)
			amount += (int)(caster.SpellBaseHealingBonusDone(GetSpellInfo().GetSchoolMask()) * 10.0f);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		Unit caster = eventInfo.GetDamageInfo().GetVictim();
		Unit target = eventInfo.GetDamageInfo().GetAttacker();

		if (caster && target)
			caster.CastSpell(target, MageSpells.Chilled, true);
	}
}