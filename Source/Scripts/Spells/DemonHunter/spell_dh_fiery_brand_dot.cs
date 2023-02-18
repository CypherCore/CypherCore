// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(207771)]
public class spell_dh_fiery_brand_dot : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void PeriodicTick(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster == null || !caster.HasAura(DemonHunterSpells.BURNING_ALIVE))
			return;

		var unitList = new List<Unit>();
		GetTarget().GetAnyUnitListInRange(unitList, 8.0f);

		foreach (var target in unitList)
			if (!target.HasAura(DemonHunterSpells.FIERY_BRAND_DOT) && !target.HasAura(DemonHunterSpells.FIERY_BRAND_MARKER) && !caster.IsFriendlyTo(target))
			{
				caster.CastSpell(target, DemonHunterSpells.FIERY_BRAND_MARKER, true);

				break;
			}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 2, AuraType.PeriodicDamage));
	}
}