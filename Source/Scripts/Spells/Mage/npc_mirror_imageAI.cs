using System;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[CreatureScript(31216)]
public class npc_mirror_imageAI : CasterAI
{
	private EventMap _events = new EventMap();

	public struct eSpells
	{
		public const uint SPELL_MAGE_FROSTBOLT = 59638;
		public const uint SPELL_MAGE_FIREBALL = 133;
		public const uint SPELL_MAGE_ARCANE_BLAST = 30451;
		public const uint SPELL_MAGE_GLYPH = 63093;
		public const uint SPELL_INITIALIZE_IMAGES = 102284;
		public const uint SPELL_CLONE_CASTER = 60352;
		public const uint SPELL_INHERIT_MASTER_THREAT = 58838;
	}

	public npc_mirror_imageAI(Creature creature) : base(creature)
	{
	}

	public override void IsSummonedBy(WorldObject owner)
	{
		if (owner == null || !owner.IsPlayer())
		{
			return;
		}

		if (!me.HasUnitState(UnitState.Follow))
		{
			me.GetMotionMaster().Clear();
			me.GetMotionMaster().MoveFollow(owner.ToUnit(), SharedConst.PetFollowDist, me.GetFollowAngle(), MovementSlot.Active);
		}

		// me->SetMaxPower(me->GetPowerType(), owner->GetMaxPower(me->GetPowerType()));
		me.SetFullPower(me.GetPowerType());
		me.SetMaxHealth(owner.ToUnit().GetMaxHealth());
		me.SetHealth(owner.ToUnit().GetHealth());
		me.SetReactState(ReactStates.Defensive);

		me.CastSpell(owner, eSpells.SPELL_INHERIT_MASTER_THREAT, true);

		// here mirror image casts on summoner spell (not present in client dbc) 49866
		// here should be auras (not present in client dbc): 35657, 35658, 35659, 35660 selfcasted by mirror images (stats related?)

		for (uint attackType = 0; attackType < (int)WeaponAttackType.Max; ++attackType)
		{
			WeaponAttackType attackTypeEnum = (WeaponAttackType)attackType;
			me.SetBaseWeaponDamage(attackTypeEnum, WeaponDamageRange.MaxDamage, owner.ToUnit().GetWeaponDamageRange(attackTypeEnum, WeaponDamageRange.MaxDamage));
			me.SetBaseWeaponDamage(attackTypeEnum, WeaponDamageRange.MinDamage, owner.ToUnit().GetWeaponDamageRange(attackTypeEnum, WeaponDamageRange.MinDamage));
		}

		me.UpdateAttackPowerAndDamage();
	}

	public override void JustEngagedWith(Unit who)
	{
		Unit owner = me.GetOwner();
		if (owner == null)
		{
			return;
		}

		Player ownerPlayer = owner.ToPlayer();
		if (ownerPlayer == null)
		{
			return;
		}

		var spellId = eSpells.SPELL_MAGE_FROSTBOLT;
		switch (ownerPlayer.GetPrimarySpecialization())
		{
			case TalentSpecialization.MageArcane:
				spellId = eSpells.SPELL_MAGE_ARCANE_BLAST;
				break;
			case TalentSpecialization.MageFire:
				spellId = eSpells.SPELL_MAGE_FIREBALL;
				break;
			default:
				break;
		}

		_events.ScheduleEvent(spellId, TimeSpan.Zero); ///< Schedule cast
		me.GetMotionMaster().Clear();
	}

	public override void EnterEvadeMode(EvadeReason UnnamedParameter)
	{
		if (me.IsInEvadeMode() || !me.IsAlive())
		{
			return;
		}

		Unit owner = me.GetOwner();

		me.CombatStop(true);
		if (owner != null && !me.HasUnitState(UnitState.Follow))
		{
			me.GetMotionMaster().Clear();
			me.GetMotionMaster().MoveFollow(owner.ToUnit(), SharedConst.PetFollowDist, me.GetFollowAngle(), MovementSlot.Active);
		}
	}

	public override void Reset()
	{
		Unit owner = me.GetOwner();
		if (owner != null)
		{
			owner.CastSpell(me, eSpells.SPELL_INITIALIZE_IMAGES, true);
			owner.CastSpell(me, eSpells.SPELL_CLONE_CASTER, true);
		}
	}



	public override bool CanAIAttack(Unit target)
	{
		/// Am I supposed to attack this target? (ie. do not attack polymorphed target)
		return target != null && !target.HasBreakableByDamageCrowdControlAura();
	}

	public override void UpdateAI(uint diff)
	{
		_events.Update(diff);

		Unit l_Victim = me.GetVictim();
		if (l_Victim != null)
		{
			if (CanAIAttack(l_Victim))
			{
				/// If not already casting, cast! ("I'm a cast machine")
				if (!me.HasUnitState(UnitState.Casting))
				{
					uint spellId = _events.ExecuteEvent();
					if (_events.ExecuteEvent() != 0)
					{
						DoCast(spellId);
						var castTime = me.GetCurrentSpellCastTime(spellId);
						_events.ScheduleEvent(spellId, TimeSpan.FromSeconds(5), Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None).ProcCooldown);
					}
				}
			}
			else
			{
				/// My victim has changed state, I shouldn't attack it anymore
				if (me.HasUnitState(UnitState.Casting))
				{
					me.CastStop();
				}

				me.GetAI().EnterEvadeMode();
			}
		}
		else
		{
			/// Let's choose a new target
			Unit target = me.SelectVictim();
			if (target == null)
			{
				/// No target? Let's see if our owner has a better target for us
				Unit owner = me.GetOwner();
				if (owner != null)
				{
					Unit ownerVictim = owner.GetVictim();
					if (ownerVictim != null && me.CanCreatureAttack(ownerVictim))
					{
						target = ownerVictim;
					}
				}
			}

			if (target != null)
			{
				me.GetAI().AttackStart(target);
			}
		}
	}
}