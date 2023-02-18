// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(8936)]
public class spell_dru_regrowth : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DruidSpells.REGROWTH, DruidSpells.BLOODTALONS, DruidSpells.BLOODTALONS_TRIGGERED, DruidSpells.MOMENT_OF_CLARITY, DruidSpells.CLEARCASTING);
	}

	private void HandleHealEffect(uint UnnamedParameter)
	{
		if (GetCaster().HasAura(DruidSpells.BLOODTALONS))
			GetCaster().AddAura(DruidSpells.BLOODTALONS_TRIGGERED, GetCaster());

		var clearcasting = GetCaster().GetAura(DruidSpells.CLEARCASTING);

		if (clearcasting != null)
		{
			if (GetCaster().HasAura(DruidSpells.MOMENT_OF_CLARITY))
			{
				var amount = clearcasting.GetEffect(0).GetAmount();
				clearcasting.GetEffect(0).SetAmount(amount - 1);

				if (amount == -102)
					GetCaster().RemoveAurasDueToSpell(DruidSpells.CLEARCASTING);
			}
			else
			{
				GetCaster().RemoveAurasDueToSpell(DruidSpells.CLEARCASTING);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHealEffect, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}