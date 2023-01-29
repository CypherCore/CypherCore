// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.AI
{
    public class BossAI : ScriptedAI
	{
		private uint _bossId;
		public InstanceScript Instance { get; set; }
		public SummonList Summons { get; set; }

        public BossAI(Creature creature, uint bossId) : base(creature)
		{
			Instance = creature.GetInstanceScript();
			Summons  = new SummonList(creature);
			_bossId  = bossId;

			if (Instance != null)
				SetBoundary(Instance.GetBossBoundary(bossId));

			_scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));
		}

		public void _Reset()
		{
			if (!me.IsAlive())
				return;

			me.SetCombatPulseDelay(0);
			me.ResetLootMode();
			Events.Reset();
			Summons.DespawnAll();
			_scheduler.CancelAll();

			if (Instance != null &&
			    Instance.GetBossState(_bossId) != EncounterState.Done)
				Instance.SetBossState(_bossId, EncounterState.NotStarted);
		}

		public void _JustDied()
		{
			Events.Reset();
			Summons.DespawnAll();
			_scheduler.CancelAll();

			if (Instance != null)
				Instance.SetBossState(_bossId, EncounterState.Done);
		}

		public void _JustEngagedWith(Unit who)
		{
			if (Instance != null)
			{
				// bosses do not respawn, check only on enter combat
				if (!Instance.CheckRequiredBosses(_bossId, who.ToPlayer()))
				{
					EnterEvadeMode(EvadeReason.SequenceBreak);

					return;
				}

				Instance.SetBossState(_bossId, EncounterState.InProgress);
			}

			me.SetCombatPulseDelay(5);
			me.SetActive(true);
			DoZoneInCombat();
			ScheduleTasks();
		}

		public void TeleportCheaters()
		{
			float x, y, z;
			me.GetPosition(out x, out y, out z);

			foreach (var pair in me.GetCombatManager().GetPvECombatRefs())
			{
				Unit target = pair.Value.GetOther(me);

				if (target.IsControlledByPlayer() &&
				    !IsInBoundary(target))
					target.NearTeleportTo(x, y, z, 0);
			}
		}

		private void ForceCombatStopForCreatureEntry(uint entry, float maxSearchRange = 250.0f, bool reset = true)
		{
			Log.outDebug(LogFilter.ScriptsAi, $"BossAI::ForceStopCombatForCreature: called on {me.GetGUID()}. Debug info: {me.GetDebugInfo()}");

			List<Creature> creatures = me.GetCreatureListWithEntryInGrid(entry, maxSearchRange);

			foreach (Creature creature in creatures)
			{
				creature.CombatStop(true);
				creature.DoNotReacquireSpellFocusTarget();
				creature.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);

				if (reset)
				{
					creature.LoadCreaturesAddon();
					creature.SetTappedBy(null);
					creature.ResetPlayerDamageReq();
					creature.SetLastDamagedTime(0);
					creature.SetCannotReachTarget(false);
				}
			}
		}

		public override void JustSummoned(Creature summon)
		{
			Summons.Summon(summon);

			if (me.IsEngaged())
				DoZoneInCombat(summon);
		}

		public override void SummonedCreatureDespawn(Creature summon)
		{
			Summons.Despawn(summon);
		}

		public override void UpdateAI(uint diff)
		{
			if (!UpdateVictim())
				return;

			Events.Update(diff);

			if (me.HasUnitState(UnitState.Casting))
				return;


			Events.ExecuteEvents(eventId =>
			                      {
				                      ExecuteEvent(eventId);

				                      if (me.HasUnitState(UnitState.Casting))
					                      return;
			                      });

			DoMeleeAttackIfReady();
		}

		public void _DespawnAtEvade()
		{
			_DespawnAtEvade(TimeSpan.FromSeconds(30));
		}

		public void _DespawnAtEvade(TimeSpan delayToRespawn, Creature who = null)
		{
			if (delayToRespawn < TimeSpan.FromSeconds(2))
			{
				Log.outError(LogFilter.ScriptsAi, $"BossAI::_DespawnAtEvade: called with delay of {delayToRespawn} seconds, defaulting to 2 (me: {me.GetGUID()})");
				delayToRespawn = TimeSpan.FromSeconds(2);
			}

			if (!who)
				who = me;

			TempSummon whoSummon = who.ToTempSummon();

			if (whoSummon)
			{
				Log.outWarn(LogFilter.ScriptsAi, $"BossAI::_DespawnAtEvade: called on a temporary summon (who: {who.GetGUID()})");
				whoSummon.UnSummon();

				return;
			}

			who.DespawnOrUnsummon(TimeSpan.Zero, delayToRespawn);

			if (Instance != null &&
			    who == me)
				Instance.SetBossState(_bossId, EncounterState.Fail);
		}

		public virtual void ExecuteEvent(uint eventId)
		{
		}

		public virtual void ScheduleTasks()
		{
		}

		public override void Reset()
		{
			_Reset();
		}

		public override void JustEngagedWith(Unit who)
		{
			_JustEngagedWith(who);
		}

		public override void JustDied(Unit killer)
		{
			_JustDied();
		}

		public override void JustReachedHome()
		{
			_JustReachedHome();
		}

		public override bool CanAIAttack(Unit victim)
		{
			return IsInBoundary(victim);
		}

		public void _JustReachedHome()
		{
			me.SetActive(false);
		}

		public uint GetBossId()
		{
			return _bossId;
		}
	}
}