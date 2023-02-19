// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(34026)]
public class spell_hun_kill_command : SpellScript, IHasSpellEffects, ISpellCheckCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private struct sspell
	{
		public const uint AnimalInstinctsReduction = 232646;
		public const uint AspectoftheBeast = 191384;
		public const uint BestialFerocity = 191413;
		public const uint BestialTenacity = 191414;
		public const uint BestialCunning = 191397;
		public const uint SpikedCollar = 53184;
		public const uint GreatStamina = 61688;
		public const uint Cornered = 53497;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(HunterSpells.KILL_COMMAND, Difficulty.None) != null)
			return false;

		return true;
	}

	public SpellCastResult CheckCast()
	{
		Unit pet       = GetCaster().GetGuardianPet();
		var  petTarget = GetExplTargetUnit();

		if (pet == null || pet.IsDead())
			return SpellCastResult.NoPet;

		// pet has a target and target is within 5 yards and target is in line of sight
		if (petTarget == null || !pet.IsWithinDist(petTarget, 40.0f, true) || !petTarget.IsWithinLOSInMap(pet))
			return SpellCastResult.DontReport;

		if (pet.HasAuraType(AuraType.ModStun) || pet.HasAuraType(AuraType.ModConfuse) || pet.HasAuraType(AuraType.ModSilence) || pet.HasAuraType(AuraType.ModFear) || pet.HasAuraType(AuraType.ModFear2))
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}

	private void HandleDummy(int effIndex)
	{
		if (GetCaster().IsPlayer())
		{
			Unit pet = GetCaster().GetGuardianPet();

			if (pet != null)
			{
				if (!pet)
					return;

				if (!GetExplTargetUnit())
					return;

				var target = GetExplTargetUnit();
				var player = GetCaster().ToPlayer();

				pet.CastSpell(GetExplTargetUnit(), HunterSpells.KILL_COMMAND_TRIGGER, true);

				if (pet.GetVictim())
				{
					pet.AttackStop();
					pet.ToCreature().GetAI().AttackStart(GetExplTargetUnit());
				}
				else
				{
					pet.ToCreature().GetAI().AttackStart(GetExplTargetUnit());
				}
				//pet->CastSpell(GetExplTargetUnit(), KILL_COMMAND_CHARGE, true);

				//191384 Aspect of the Beast
				if (GetCaster().HasAura(sspell.AspectoftheBeast))
				{
					if (pet.HasAura(sspell.SpikedCollar))
						player.CastSpell(target, sspell.BestialFerocity, true);

					if (pet.HasAura(sspell.GreatStamina))
						pet.CastSpell(pet, sspell.BestialTenacity, true);

					if (pet.HasAura(sspell.Cornered))
						player.CastSpell(target, sspell.BestialCunning, true);
				}
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}
}