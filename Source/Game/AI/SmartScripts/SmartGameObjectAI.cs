// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.AI
{
    public class SmartGameObjectAI : GameObjectAI
	{
		// Gossip
		private bool _gossipReturn;
		private readonly SmartScript _script = new();

		public SmartGameObjectAI(GameObject go) : base(go)
		{
		}

		public override void UpdateAI(uint diff)
		{
			GetScript().OnUpdate(diff);
		}

		public override void InitializeAI()
		{
			GetScript().OnInitialize(me);

			// do not call respawn event if go is not spawned
			if (me.IsSpawned())
				GetScript().ProcessEventsFor(SmartEvents.Respawn);
		}

		public override void Reset()
		{
			GetScript().OnReset();
		}

		public override bool OnGossipHello(Player player)
		{
			_gossipReturn = false;
			GetScript().ProcessEventsFor(SmartEvents.GossipHello, player, 0, 0, false, null, me);

			return _gossipReturn;
		}

		public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
		{
			_gossipReturn = false;
			GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, menuId, gossipListId, false, null, me);

			return _gossipReturn;
		}

		public override bool OnGossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
		{
			return false;
		}

		public override void OnQuestAccept(Player player, Quest quest)
		{
			GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id, 0, false, null, me);
		}

		public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
		{
			GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt, false, null, me);
		}

		public override bool OnReportUse(Player player)
		{
			_gossipReturn = false;
			GetScript().ProcessEventsFor(SmartEvents.GossipHello, player, 1, 0, false, null, me);

			return _gossipReturn;
		}

		public override void Destroyed(WorldObject attacker, uint eventId)
		{
			GetScript().ProcessEventsFor(SmartEvents.Death, attacker != null ? attacker.ToUnit() : null, eventId, 0, false, null, me);
		}

		public override void SetData(uint id, uint value)
		{
			SetData(id, value, null);
		}

		public void SetData(uint id, uint value, Unit invoker)
		{
			GetScript().ProcessEventsFor(SmartEvents.DataSet, invoker, id, value);
		}

		public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
		{
			GetScript().SetTimedActionList(e, entry, invoker);
		}

		public override void OnGameEvent(bool start, ushort eventId)
		{
			GetScript().ProcessEventsFor(start ? SmartEvents.GameEventStart : SmartEvents.GameEventEnd, null, eventId);
		}

		public override void OnLootStateChanged(uint state, Unit unit)
		{
			GetScript().ProcessEventsFor(SmartEvents.GoLootStateChanged, unit, state);
		}

		public override void EventInform(uint eventId)
		{
			GetScript().ProcessEventsFor(SmartEvents.GoEventInform, null, eventId);
		}

		public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
		{
			GetScript().ProcessEventsFor(SmartEvents.SpellHit, caster.ToUnit(), 0, 0, false, spellInfo);
		}

		public override void JustSummoned(Creature creature)
		{
			GetScript().ProcessEventsFor(SmartEvents.SummonedUnit, creature);
		}

		public override void SummonedCreatureDies(Creature summon, Unit killer)
		{
			GetScript().ProcessEventsFor(SmartEvents.SummonedUnitDies, summon);
		}

		public override void SummonedCreatureDespawn(Creature unit)
		{
			GetScript().ProcessEventsFor(SmartEvents.SummonDespawned, unit, unit.GetEntry());
		}

		public void SetGossipReturn(bool val)
		{
			_gossipReturn = val;
		}

		public SmartScript GetScript()
		{
			return _script;
		}
	}
}