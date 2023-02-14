// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 131347 - Glide
internal class spell_dh_glide : SpellScript, ISpellCheckCast, ISpellBeforeCast
{
	public void BeforeCast()
	{
		var caster = GetCaster().ToPlayer();

		if (!caster)
			return;

		caster.CastSpell(caster, DemonHunterSpells.GlideKnockback, true);
		caster.CastSpell(caster, DemonHunterSpells.GlideDuration, true);

		caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.VengefulRetreatTrigger, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
		caster.GetSpellHistory().StartCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.FelRush, GetCastDifficulty()), 0, null, false, TimeSpan.FromMilliseconds(250));
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DemonHunterSpells.GlideKnockback, DemonHunterSpells.GlideDuration, DemonHunterSpells.VengefulRetreatTrigger, DemonHunterSpells.FelRush);
	}

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster();

		if (caster.IsMounted() ||
		    caster.GetVehicleBase())
			return SpellCastResult.DontReport;

		if (!caster.IsFalling())
			return SpellCastResult.NotOnGround;

		return SpellCastResult.SpellCastOk;
	}
}