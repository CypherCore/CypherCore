using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 45472 Parachute
internal class spell_gen_parachute : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.Parachute, GenericSpellIds.ParachuteBuff);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var target = GetTarget().ToPlayer();

		if (target)
			if (target.IsFalling())
			{
				target.RemoveAurasDueToSpell(GenericSpellIds.Parachute);
				target.CastSpell(target, GenericSpellIds.ParachuteBuff, true);
			}
	}
}