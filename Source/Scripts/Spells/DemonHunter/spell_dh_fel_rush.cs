// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(195072)]
public class spell_dh_fel_rush : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FEL_RUSH_DASH, Difficulty.None) != null)
			return false;

		if (Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FEL_RUSH_AIR, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandleDashGround(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			if (!caster.IsFalling() || caster.IsInWater())
			{
				caster.RemoveAurasDueToSpell(DemonHunterSpells.GLIDE);
				caster.CastSpell(caster, DemonHunterSpells.FEL_RUSH_DASH, true);

				if (GetHitUnit())
					caster.CastSpell(GetHitUnit(), DemonHunterSpells.FEL_RUSH_DAMAGE, true);

				if (caster.HasAura(ShatteredSoulsSpells.MOMENTUM))
					caster.CastSpell(ShatteredSoulsSpells.MOMENTUM_BUFF, true);
			}

			caster.GetSpellHistory().AddCooldown(GetSpellInfo().Id, 0, TimeSpan.FromMicroseconds(750));
		}
	}

	private void HandleDashAir(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			if (caster.IsFalling())
			{
				caster.RemoveAurasDueToSpell(DemonHunterSpells.GLIDE);
				caster.SetDisableGravity(true);
				caster.CastSpell(caster, DemonHunterSpells.FEL_RUSH_AIR, true);

				if (GetHitUnit())
					caster.CastSpell(GetHitUnit(), DemonHunterSpells.FEL_RUSH_DAMAGE, true);

				if (caster.HasAura(ShatteredSoulsSpells.MOMENTUM))
					caster.CastSpell(ShatteredSoulsSpells.MOMENTUM_BUFF, true);

				caster.GetSpellHistory().AddCooldown(GetSpellInfo().Id, 0, TimeSpan.FromMicroseconds(750));
			}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDashGround, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleDashAir, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}