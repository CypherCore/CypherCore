using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 43265 - Death and Decay
internal class spell_dk_death_and_decay_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 2, AuraType.PeriodicDummy));
	}

	private void HandleDummyTick(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster)
			caster.CastSpell(GetTarget(), DeathKnightSpells.DeathAndDecayDamage, new CastSpellExtraArgs(aurEff));
	}
}