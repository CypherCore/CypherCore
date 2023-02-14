// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_tame_beast : SpellScript, ISpellCheckCast
{
	private static readonly uint[] CallPetSpellIds =
	{
		883, 83242, 83243, 83244, 83245
	};

	public SpellCastResult CheckCast()
	{
		var caster = GetCaster().ToPlayer();

		if (caster == null)
			return SpellCastResult.DontReport;

		if (!GetExplTargetUnit())
			return SpellCastResult.BadImplicitTargets;

		var target = GetExplTargetUnit().ToCreature();

		if (target)
		{
			if (target.GetLevel() > caster.GetLevel())
				return SpellCastResult.Highlevel;

			// use SMSG_PET_TAME_FAILURE?
			if (!target.GetCreatureTemplate().IsTameable(caster.CanTameExoticPets()))
				return SpellCastResult.BadTargets;

			var petStable = caster.GetPetStable();

			if (petStable != null)
			{
				if (petStable.CurrentPetIndex.HasValue)
					return SpellCastResult.AlreadyHaveSummon;

				var freeSlotIndex = Array.FindIndex(petStable.ActivePets, petInfo => petInfo == null);

				if (freeSlotIndex == -1)
				{
					caster.SendTameFailure(PetTameResult.TooMany);

					return SpellCastResult.DontReport;
				}

				// Check for known Call Pet X spells
				if (!caster.HasSpell(CallPetSpellIds[freeSlotIndex]))
				{
					caster.SendTameFailure(PetTameResult.TooMany);

					return SpellCastResult.DontReport;
				}
			}

			if (!caster.GetCharmedGUID().IsEmpty())
				return SpellCastResult.AlreadyHaveCharm;

			if (!target.GetOwnerGUID().IsEmpty())
			{
				caster.SendTameFailure(PetTameResult.CreatureAlreadyOwned);

				return SpellCastResult.DontReport;
			}
		}
		else
		{
			return SpellCastResult.BadImplicitTargets;
		}

		return SpellCastResult.SpellCastOk;
	}
}