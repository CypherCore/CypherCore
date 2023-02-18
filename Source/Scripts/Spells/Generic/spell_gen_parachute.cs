// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
				target.RemoveAura(GenericSpellIds.Parachute);
				target.CastSpell(target, GenericSpellIds.ParachuteBuff, true);
			}
	}
}