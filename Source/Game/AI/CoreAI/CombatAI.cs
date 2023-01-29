// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
	public class CombatAI : CreatureAI
	{
		protected List<uint> _spells = new();

		public CombatAI(Creature c) : base(c)
		{
		}

		public override void InitializeAI()
		{
			for (var i = 0; i < SharedConst.MaxCreatureSpells; ++i)
				if (me.Spells[i] != 0 &&
				    Global.SpellMgr.HasSpellInfo(me.Spells[i], me.GetMap().GetDifficultyID()))
					_spells.Add(me.Spells[i]);

			base.InitializeAI();
		}

		public override void Reset()
		{
			Events.Reset();
		}

		public override void JustDied(Unit killer)
		{
			foreach (var id in _spells)
			{
				AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());

				if (info != null &&
				    info.Condition == AICondition.Die)
					me.CastSpell(killer, id, true);
			}
		}

		public override void JustEngagedWith(Unit victim)
		{
			foreach (var id in _spells)
			{
				AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());

				if (info != null)
				{
					if (info.Condition == AICondition.Aggro)
						me.CastSpell(victim, id, false);
					else if (info.Condition == AICondition.Combat)
						Events.ScheduleEvent(id, info.Cooldown, info.Cooldown * 2);
				}
			}
		}

		public override void UpdateAI(uint diff)
		{
			if (!UpdateVictim())
				return;

			Events.Update(diff);

			if (me.HasUnitState(UnitState.Casting))
				return;

			uint spellId = Events.ExecuteEvent();

			if (spellId != 0)
			{
				DoCast(spellId);
				AISpellInfoType info = GetAISpellInfo(spellId, me.GetMap().GetDifficultyID());

				if (info != null)
					Events.ScheduleEvent(spellId, info.Cooldown, info.Cooldown * 2);
			}
			else
			{
				DoMeleeAttackIfReady();
			}
		}

		public override void SpellInterrupted(uint spellId, uint unTimeMs)
		{
			Events.RescheduleEvent(spellId, TimeSpan.FromMilliseconds(unTimeMs));
		}
	}

	public class AggressorAI : CreatureAI
	{
		public AggressorAI(Creature c) : base(c)
		{
		}

		public override void UpdateAI(uint diff)
		{
			if (!UpdateVictim())
				return;

			DoMeleeAttackIfReady();
		}
	}

	public class CasterAI : CombatAI
	{
		private float _attackDistance;

		public CasterAI(Creature creature) : base(creature)
		{
			_attackDistance = SharedConst.MeleeRange;
		}

		public override void InitializeAI()
		{
			base.InitializeAI();

			_attackDistance = 30.0f;

			foreach (var id in _spells)
			{
				AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());

				if (info != null &&
				    info.Condition == AICondition.Combat &&
				    _attackDistance > info.MaxRange)
					_attackDistance = info.MaxRange;
			}

			if (_attackDistance == 30.0f)
				_attackDistance = SharedConst.MeleeRange;
		}

		public override void AttackStart(Unit victim)
		{
			AttackStartCaster(victim, _attackDistance);
		}

		public override void JustEngagedWith(Unit victim)
		{
			if (_spells.Empty())
				return;

			int  spell = (int)(RandomHelper.Rand32() % _spells.Count);
			uint count = 0;

			foreach (var id in _spells)
			{
				AISpellInfoType info = GetAISpellInfo(id, me.GetMap().GetDifficultyID());

				if (info != null)
				{
					if (info.Condition == AICondition.Aggro)
					{
						me.CastSpell(victim, id, false);
					}
					else if (info.Condition == AICondition.Combat)
					{
						TimeSpan cooldown = info.RealCooldown;

						if (count == spell)
						{
							DoCast(_spells[spell]);
							cooldown += TimeSpan.FromMilliseconds(me.GetCurrentSpellCastTime(id));
						}

						Events.ScheduleEvent(id, cooldown);
					}
				}
			}
		}

		public override void UpdateAI(uint diff)
		{
			if (!UpdateVictim())
				return;

			Events.Update(diff);

			if (me.GetVictim().HasBreakableByDamageCrowdControlAura(me))
			{
				me.InterruptNonMeleeSpells(false);

				return;
			}

			if (me.HasUnitState(UnitState.Casting))
				return;

			uint spellId = Events.ExecuteEvent();

			if (spellId != 0)
			{
				DoCast(spellId);
				uint            casttime = (uint)me.GetCurrentSpellCastTime(spellId);
				AISpellInfoType info     = GetAISpellInfo(spellId, me.GetMap().GetDifficultyID());

				if (info != null)
					Events.ScheduleEvent(spellId, TimeSpan.FromMilliseconds(casttime != 0 ? casttime : 500) + info.RealCooldown);
			}
		}
	}
}