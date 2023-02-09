using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 40119 Knockdown Fel Cannon: The Aggro Burst
internal class spell_q11010_q11102_q11023_aggro_burst : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		Unit target = GetTarget();

		if (target)
			// On each tick cast Choose Loc to trigger summon
			target.CastSpell(target, QuestSpellIds.ChooseLoc);
	}
}