using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 9712 - Thaumaturgy Channel
internal class spell_q2203_thaumaturgy_channel : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(QuestSpellIds.ThaumaturgyChannel);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicTriggerSpell));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		PreventDefaultAction();
		Unit caster = GetCaster();

		if (caster)
			caster.CastSpell(caster, QuestSpellIds.ThaumaturgyChannel, false);
	}
}