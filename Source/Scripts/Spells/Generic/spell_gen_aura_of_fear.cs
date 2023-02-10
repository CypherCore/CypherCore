using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 28313 - Aura of Fear
internal class spell_gen_aura_of_fear : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return !spellInfo.GetEffects().Empty() && ValidateSpellInfo(spellInfo.GetEffect(0).TriggerSpell);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
	}

	private void PeriodicTick(AuraEffect aurEff)
	{
		PreventDefaultAction();

		if (!RandomHelper.randChance(GetSpellInfo().ProcChance))
			return;

		GetTarget().CastSpell(null, aurEff.GetSpellEffectInfo().TriggerSpell, true);
	}
}