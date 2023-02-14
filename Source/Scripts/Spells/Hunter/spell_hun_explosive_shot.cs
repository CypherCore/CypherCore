// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 212431 - Explosive Shot
class spell_hun_explosive_shot : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.ExplosiveShotDamage);
	}

	void HandlePeriodic(AuraEffect aurEff)
	{
		GetCaster()?.CastSpell(GetTarget(), HunterSpells.ExplosiveShotDamage, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
	}
}