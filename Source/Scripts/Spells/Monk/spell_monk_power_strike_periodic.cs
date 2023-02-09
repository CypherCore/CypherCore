using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(121817)]
public class spell_monk_power_strike_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_POWER_STRIKES_AURA);
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		GetTarget().CastSpell(GetTarget(), MonkSpells.SPELL_MONK_POWER_STRIKES_AURA, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
	}
}