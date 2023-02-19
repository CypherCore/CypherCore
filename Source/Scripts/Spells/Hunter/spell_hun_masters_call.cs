// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_masters_call : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return !spellInfo.GetEffects().Empty() && ValidateSpellInfo(HunterSpells.MastersCallTriggered, (uint)spellInfo.GetEffect(0).CalcValue());
	}

	public override bool Load()
	{
		return GetCaster().IsPlayer();
	}

	public SpellCastResult CheckCast()
	{
		var pet = GetCaster().ToPlayer().GetGuardianPet();

		if (pet == null ||
		    !pet.IsPet() ||
		    !pet.IsAlive())
			return SpellCastResult.NoPet;

		// Do a mini Spell::CheckCasterAuras on the pet, no other way of doing this
		var result   = SpellCastResult.SpellCastOk;
		var unitflag = (UnitFlags)(uint)pet.m_unitData.Flags;

		if (!pet.GetCharmerGUID().IsEmpty())
			result = SpellCastResult.Charmed;
		else if (unitflag.HasAnyFlag(UnitFlags.Stunned))
			result = SpellCastResult.Stunned;
		else if (unitflag.HasAnyFlag(UnitFlags.Fleeing))
			result = SpellCastResult.Fleeing;
		else if (unitflag.HasAnyFlag(UnitFlags.Confused))
			result = SpellCastResult.Confused;

		if (result != SpellCastResult.SpellCastOk)
			return result;

		var target = GetExplTargetUnit();

		if (!target)
			return SpellCastResult.BadTargets;

		if (!pet.IsWithinLOSInMap(target))
			return SpellCastResult.LineOfSight;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(int effIndex)
	{
		GetCaster().ToPlayer().GetPet().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
	}

	private void HandleScriptEffect(int effIndex)
	{
		GetHitUnit().CastSpell((Unit)null, HunterSpells.MastersCallTriggered, true);
	}
}