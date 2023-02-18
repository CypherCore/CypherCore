// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(84721)]
public class spell_mage_frozen_orb : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		caster.CastSpell(target, MageSpells.CHILLED, true);

		// Fingers of Frost
		if (caster.HasSpell(MageSpells.FINGERS_OF_FROST))
		{
			var fingersFrostChance = 10.0f;

			if (caster.HasAura(MageSpells.FROZEN_TOUCH))
			{
				var frozenEff0 = caster.GetAuraEffect(MageSpells.FROZEN_TOUCH, 0);

				if (frozenEff0 != null)
				{
					var pct = frozenEff0.GetAmount();
					MathFunctions.AddPct(ref fingersFrostChance, pct);
				}
			}

			if (RandomHelper.randChance(fingersFrostChance))
			{
				caster.CastSpell(caster, MageSpells.FINGERS_OF_FROST_VISUAL_UI, true);
				caster.CastSpell(caster, MageSpells.FINGERS_OF_FROST_AURA, true);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}