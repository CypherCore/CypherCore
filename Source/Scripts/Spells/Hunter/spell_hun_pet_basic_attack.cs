using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[] { 49966, 17253, 16827 })]
public class spell_hun_pet_basic_attack : SpellScript, IHasSpellEffects, ISpellCheckCast
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	public SpellCastResult CheckCast()
	{
		Unit caster = GetCaster();

		if (caster == null)
		{
			return SpellCastResult.DontReport;
		}

		Unit owner = caster.GetOwner();

		if (owner == null)
		{
			return SpellCastResult.DontReport;
		}

		Unit target = GetExplTargetUnit();

		if (target == null)
		{
			return SpellCastResult.DontReport;
		}

		if (owner.HasSpell(HunterSpells.SPELL_HUNTER_BLINK_STRIKES))
		{
			if (owner.ToPlayer().GetSpellHistory().HasCooldown(HunterSpells.SPELL_HUNTER_BLINK_STRIKES) && caster.GetDistance(target) > 10.0f)
			{
				return SpellCastResult.OutOfRange;
			}

			if ((caster.HasAuraType(AuraType.ModRoot) || caster.HasAuraType(AuraType.ModStun)) && caster.GetDistance(target) > 5.0f)
			{
				return SpellCastResult.Rooted;
			}

			if (!owner.ToPlayer().GetSpellHistory().HasCooldown(HunterSpells.SPELL_HUNTER_BLINK_STRIKES) && target.IsWithinLOSInMap(caster) && caster.GetDistance(target) > 10.0f && caster.GetDistance(target) < 30.0f && !caster.HasAuraType(AuraType.ModStun))
			{
				caster.CastSpell(target, HunterSpells.SPELL_HUNTER_BLINK_STRIKES_TELEPORT, true);

				if (caster.ToCreature().IsAIEnabled() && caster.ToPet())
				{
					caster.ToPet().ClearUnitState(UnitState.Follow);
					if (caster.ToPet().GetVictim())
					{
						caster.ToPet().AttackStop();
					}
					caster.GetMotionMaster().Clear();
					caster.ToPet().GetCharmInfo().SetIsCommandAttack(true);
					caster.ToPet().GetCharmInfo().SetIsAtStay(false);
					caster.ToPet().GetCharmInfo().SetIsReturning(false);
					caster.ToPet().GetCharmInfo().SetIsFollowing(false);

					caster.ToCreature().GetAI().AttackStart(target);
				}

				owner.ToPlayer().GetSpellHistory().AddCooldown(HunterSpells.SPELL_HUNTER_BLINK_STRIKES, 0, TimeSpan.FromSeconds(20));
			}
		}
		return SpellCastResult.SpellCastOk;
	}

	private void HandleDamage(uint UnnamedParameter)
	{
		Pet pet = GetCaster().ToPet();
		if (pet != null)
		{
			Unit owner = GetCaster().GetOwner();
			if (owner != null)
			{
				Unit target = GetHitUnit();
				if (target == null)
				{
					return;
				}

				// (1.5 * 1 * 1 * (Ranged attack power * 0.333) * (1 + $versadmg))
				var dmg = owner.m_unitData.RangedAttackPower * 0.333f;

				SpellInfo CostModifier = Global.SpellMgr.GetSpellInfo(HunterSpells.SPELL_HUNTER_BASIC_ATTACK_COST_MODIFIER, Difficulty.None);
				SpellInfo SpikedCollar = Global.SpellMgr.GetSpellInfo(HunterSpells.SPELL_HUNTER_SPIKED_COLLAR, Difficulty.None);

				// Increases the damage done by your pet's Basic Attacks by 10%
				if (pet.HasAura(HunterSpells.SPELL_HUNTER_SPIKED_COLLAR) && SpikedCollar != null)
				{
					MathFunctions.AddPct(ref dmg, SpikedCollar.GetEffect(0).BasePoints);
				}

				// Deals 100% more damage and costs 100% more Focus when your pet has 50 or more Focus.
				if (pet.GetPower(PowerType.Focus) + 25 >= 50)
				{
					if (CostModifier != null)
					{
						dmg += MathFunctions.CalculatePct(dmg, CostModifier.GetEffect(1).BasePoints);
					}

					pet.EnergizeBySpell(pet, GetSpellInfo(), 25, PowerType.Focus);
					// pet->EnergizeBySpell(pet, GetSpellInfo()->Id, -25, PowerType.Focus);
				}

				dmg *= pet.GetPctModifierValue(UnitMods.DamageMainHand, UnitModifierPctType.Total);

				if (target != null)
				{
					dmg = pet.SpellDamageBonusDone(target, GetSpellInfo(), dmg, DamageEffectType.Direct, GetEffectInfo(0));
					dmg = target.SpellDamageBonusTaken(pet, GetSpellInfo(), dmg, DamageEffectType.Direct);
				}

				SetHitDamage(dmg);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}