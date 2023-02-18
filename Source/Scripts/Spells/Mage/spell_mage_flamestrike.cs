// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(2120)]
public class spell_mage_flamestrike : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleOnHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.HasAura(MageSpells.HOT_STREAK))
		{
			caster.RemoveAura(MageSpells.HOT_STREAK);

			if (caster.HasAura(MageSpells.PYROMANIAC))
			{
				var pyromaniacEff0 = caster.GetAuraEffect(MageSpells.PYROMANIAC, 0);

				if (pyromaniacEff0 != null)
					if (RandomHelper.randChance(pyromaniacEff0.GetAmount()))
					{
						if (caster.HasAura(MageSpells.HEATING_UP))
							caster.RemoveAura(MageSpells.HEATING_UP);

						caster.CastSpell(caster, MageSpells.HOT_STREAK, true);
					}
			}
		}
	}

	public void AfterCast()
	{
		var caster = GetCaster();
		var dest   = GetExplTargetDest();

		if (caster == null || dest == null)
			return;

		if (caster.HasAura(MageSpells.FLAME_PATCH))
			caster.CastSpell(dest.GetPosition(), MageSpells.FLAME_PATCH_TRIGGER, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
	}
}