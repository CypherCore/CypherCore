// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Maps
{
	public class Notifier
	{
		public virtual void Visit(IList<WorldObject> objs)
		{
		}

		public virtual void Visit(IList<Creature> objs)
		{
		}

		public virtual void Visit(IList<AreaTrigger> objs)
		{
		}

		public virtual void Visit(IList<SceneObject> objs)
		{
		}

		public virtual void Visit(IList<Conversation> objs)
		{
		}

		public virtual void Visit(IList<GameObject> objs)
		{
		}

		public virtual void Visit(IList<DynamicObject> objs)
		{
		}

		public virtual void Visit(IList<Corpse> objs)
		{
		}

		public virtual void Visit(IList<Player> objs)
		{
		}

		public void CreatureUnitRelocationWorker(Creature c, Unit u)
		{
			if (!u.IsAlive() ||
			    !c.IsAlive() ||
			    c == u ||
			    u.IsInFlight())
				return;

			if (!c.HasUnitState(UnitState.Sightless))
			{
				if (c.IsAIEnabled() &&
				    c.CanSeeOrDetect(u, false, true))
				{
					c.GetAI().MoveInLineOfSight_Safe(u);
				}
				else
				{
					if (u.IsTypeId(TypeId.Player) &&
					    u.HasStealthAura() &&
					    c.IsAIEnabled() &&
					    c.CanSeeOrDetect(u, false, true, true))
						c.GetAI().TriggerAlert(u);
				}
			}
		}
	}

	public class Visitor
	{
		internal GridMapTypeMask _mask;

		private Notifier _notifier;

		public Visitor(Notifier notifier, GridMapTypeMask mask)
		{
			_notifier = notifier;
			_mask     = mask;
		}

		public void Visit(IList<WorldObject> collection)
		{
			_notifier.Visit(collection);
		}

		public void Visit(IList<Creature> creatures)
		{
			_notifier.Visit(creatures);
		}

		public void Visit(IList<AreaTrigger> areatriggers)
		{
			_notifier.Visit(areatriggers);
		}

		public void Visit(IList<SceneObject> sceneObjects)
		{
			_notifier.Visit(sceneObjects);
		}

		public void Visit(IList<Conversation> conversations)
		{
			_notifier.Visit(conversations);
		}

		public void Visit(IList<GameObject> gameobjects)
		{
			_notifier.Visit(gameobjects);
		}

		public void Visit(IList<DynamicObject> dynamicobjects)
		{
			_notifier.Visit(dynamicobjects);
		}

		public void Visit(IList<Corpse> corpses)
		{
			_notifier.Visit(corpses);
		}

		public void Visit(IList<Player> players)
		{
			_notifier.Visit(players);
		}
	}

	public class VisibleNotifier : Notifier
	{
		internal UpdateData i_data;

		internal Player i_player;
		internal List<Unit> i_visibleNow;
		internal List<ObjectGuid> vis_guids;

		public VisibleNotifier(Player pl)
		{
			i_player     = pl;
			i_data       = new UpdateData(pl.GetMapId());
			vis_guids    = new List<ObjectGuid>(pl._clientGUIDs);
			i_visibleNow = new List<Unit>();
		}

		public override void Visit(IList<WorldObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				WorldObject obj = objs[i];

				vis_guids.Remove(obj.GetGUID());
				i_player.UpdateVisibilityOf(obj, i_data, i_visibleNow);
			}
		}

		public void SendToSelf()
		{
			// at this moment i_clientGUIDs have guids that not iterate at grid level checks
			// but exist one case when this possible and object not out of range: transports
			Transport transport = i_player.GetTransport<Transport>();

			if (transport)
				foreach (var obj in transport.GetPassengers())
					if (vis_guids.Contains(obj.GetGUID()))
					{
						vis_guids.Remove(obj.GetGUID());

						switch (obj.GetTypeId())
						{
							case TypeId.GameObject:
								i_player.UpdateVisibilityOf(obj.ToGameObject(), i_data, i_visibleNow);

								break;
							case TypeId.Player:
								i_player.UpdateVisibilityOf(obj.ToPlayer(), i_data, i_visibleNow);

								if (!obj.IsNeedNotify(NotifyFlags.VisibilityChanged))
									obj.ToPlayer().UpdateVisibilityOf(i_player);

								break;
							case TypeId.Unit:
								i_player.UpdateVisibilityOf(obj.ToCreature(), i_data, i_visibleNow);

								break;
							case TypeId.DynamicObject:
								i_player.UpdateVisibilityOf(obj.ToDynamicObject(), i_data, i_visibleNow);

								break;
							case TypeId.AreaTrigger:
								i_player.UpdateVisibilityOf(obj.ToAreaTrigger(), i_data, i_visibleNow);

								break;
							default:
								break;
						}
					}

			foreach (var guid in vis_guids)
			{
				i_player._clientGUIDs.Remove(guid);
				i_data.AddOutOfRangeGUID(guid);

				if (guid.IsPlayer())
				{
					Player pl = Global.ObjAccessor.FindPlayer(guid);

					if (pl != null &&
					    pl.IsInWorld &&
					    !pl.IsNeedNotify(NotifyFlags.VisibilityChanged))
						pl.UpdateVisibilityOf(i_player);
				}
			}

			if (!i_data.HasData())
				return;

			UpdateObject packet;
			i_data.BuildPacket(out packet);
			i_player.SendPacket(packet);

			foreach (var obj in i_visibleNow)
				i_player.SendInitialVisiblePackets(obj);
		}
	}

	public class VisibleChangesNotifier : Notifier
	{
		private ICollection<WorldObject> i_objects;

		public VisibleChangesNotifier(ICollection<WorldObject> objects)
		{
			i_objects = objects;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				player.UpdateVisibilityOf(i_objects);

				foreach (var visionPlayer in player.GetSharedVisionList())
					if (visionPlayer.seerView == player)
						visionPlayer.UpdateVisibilityOf(i_objects);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				foreach (var visionPlayer in creature.GetSharedVisionList())
					if (visionPlayer.seerView == creature)
						visionPlayer.UpdateVisibilityOf(i_objects);
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];
				Unit          caster        = dynamicObject.GetCaster();

				if (caster)
				{
					Player pl = caster.ToPlayer();

					if (pl && pl.seerView == dynamicObject)
						pl.UpdateVisibilityOf(i_objects);
				}
			}
		}
	}

	public class PlayerRelocationNotifier : VisibleNotifier
	{
		public PlayerRelocationNotifier(Player player) : base(player)
		{
		}

		public override void Visit(IList<Player> objs)
		{
			base.Visit(objs);

			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];
				vis_guids.Remove(player.GetGUID());

				i_player.UpdateVisibilityOf(player, i_data, i_visibleNow);

				if (player.seerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
					continue;

				player.UpdateVisibilityOf(i_player);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			base.Visit(objs);

			bool relocated_for_ai = (i_player == i_player.seerView);

			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];
				vis_guids.Remove(creature.GetGUID());

				i_player.UpdateVisibilityOf(creature, i_data, i_visibleNow);

				if (relocated_for_ai && !creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
					CreatureUnitRelocationWorker(creature, i_player);
			}
		}
	}

	public class CreatureRelocationNotifier : Notifier
	{
		private Creature i_creature;

		public CreatureRelocationNotifier(Creature c)
		{
			i_creature = c;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.seerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
					player.UpdateVisibilityOf(i_creature);

				CreatureUnitRelocationWorker(i_creature, player);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			if (!i_creature.IsAlive())
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];
				CreatureUnitRelocationWorker(i_creature, creature);

				if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
					CreatureUnitRelocationWorker(creature, i_creature);
			}
		}
	}

	public class DelayedUnitRelocation : Notifier
	{
		private Cell cell;

		private Map i_map;
		private float i_radius;
		private CellCoord p;

		public DelayedUnitRelocation(Cell c, CellCoord pair, Map map, float radius)
		{
			i_map    = map;
			cell     = c;
			p        = pair;
			i_radius = radius;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player      player    = objs[i];
				WorldObject viewPoint = player.seerView;

				if (!viewPoint.IsNeedNotify(NotifyFlags.VisibilityChanged))
					continue;

				if (player != viewPoint &&
				    !viewPoint.IsPositionValid())
					continue;

				var relocate = new PlayerRelocationNotifier(player);
				Cell.VisitAllObjects(viewPoint, relocate, i_radius, false);

				relocate.SendToSelf();
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
					continue;

				CreatureRelocationNotifier relocate = new(creature);

				var c2world_relocation = new Visitor(relocate, GridMapTypeMask.AllWorld);
				var c2grid_relocation  = new Visitor(relocate, GridMapTypeMask.AllGrid);

				cell.Visit(p, c2world_relocation, i_map, creature, i_radius);
				cell.Visit(p, c2grid_relocation, i_map, creature, i_radius);
			}
		}
	}

	public class AIRelocationNotifier : Notifier
	{
		private Unit i_unit;
		private bool isCreature;

		public AIRelocationNotifier(Unit unit)
		{
			i_unit     = unit;
			isCreature = unit.IsTypeId(TypeId.Unit);
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];
				CreatureUnitRelocationWorker(creature, i_unit);

				if (isCreature)
					CreatureUnitRelocationWorker(i_unit.ToCreature(), creature);
			}
		}
	}

	public class PacketSenderRef : IDoWork<Player>
	{
		private ServerPacket Data;

		public PacketSenderRef(ServerPacket message)
		{
			Data = message;
		}

		public virtual void Invoke(Player player)
		{
			player.SendPacket(Data);
		}
	}

	public class PacketSenderOwning<T> : IDoWork<Player> where T : ServerPacket, new()
	{
		public T Data = new();

		public void Invoke(Player player)
		{
			player.SendPacket(Data);
		}
	}

	public class MessageDistDeliverer<T> : Notifier where T : IDoWork<Player>
	{
		private float i_distSq;
		private T i_packetSender;
		private PhaseShift i_phaseShift;
		private WorldObject i_source;
		private bool required3dDist;
		private Player skipped_receiver;
		private Team team;

		public MessageDistDeliverer(WorldObject src, T packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
		{
			i_source       = src;
			i_packetSender = packetSender;
			i_phaseShift   = src.GetPhaseShift();
			i_distSq       = dist * dist;

			if (own_team_only && src.IsPlayer())
				team = src.ToPlayer().GetEffectiveTeam();

			skipped_receiver = skipped;
			required3dDist   = req3dDist;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if ((!required3dDist ? player.GetExactDist2dSq(i_source) : player.GetExactDistSq(i_source)) > i_distSq)
					continue;

				// Send packet to all who are sharing the player's vision
				if (player.HasSharedVision())
					foreach (var visionPlayer in player.GetSharedVisionList())
						if (visionPlayer.seerView == player)
							SendPacket(visionPlayer);

				if (player.seerView == player ||
				    player.GetVehicle() != null)
					SendPacket(player);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if ((!required3dDist ? creature.GetExactDist2dSq(i_source) : creature.GetExactDistSq(i_source)) > i_distSq)
					continue;

				// Send packet to all who are sharing the creature's vision
				if (creature.HasSharedVision())
					foreach (var visionPlayer in creature.GetSharedVisionList())
						if (visionPlayer.seerView == creature)
							SendPacket(visionPlayer);
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];

				if (!dynamicObject.InSamePhase(i_phaseShift))
					continue;

				if ((!required3dDist ? dynamicObject.GetExactDist2dSq(i_source) : dynamicObject.GetExactDistSq(i_source)) > i_distSq)
					continue;

				// Send packet back to the caster if the caster has vision of dynamic object
				Unit caster = dynamicObject.GetCaster();

				if (caster)
				{
					Player player = caster.ToPlayer();

					if (player && player.seerView == dynamicObject)
						SendPacket(player);
				}
			}
		}

		private void SendPacket(Player player)
		{
			// never send packet to self
			if (i_source == player ||
			    (team != 0 && player.GetEffectiveTeam() != team) ||
			    skipped_receiver == player)
				return;

			if (!player.HaveAtClient(i_source))
				return;

			i_packetSender.Invoke(player);
		}
	}

	public class MessageDistDelivererToHostile<T> : Notifier where T : IDoWork<Player>
	{
		private float i_distSq;
		private T i_packetSender;
		private PhaseShift i_phaseShift;
		private Unit i_source;

		public MessageDistDelivererToHostile(Unit src, T packetSender, float dist)
		{
			i_source       = src;
			i_packetSender = packetSender;
			i_phaseShift   = src.GetPhaseShift();
			i_distSq       = dist * dist;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if (player.GetExactDist2dSq(i_source) > i_distSq)
					continue;

				// Send packet to all who are sharing the player's vision
				if (player.HasSharedVision())
					foreach (var visionPlayer in player.GetSharedVisionList())
						if (visionPlayer.seerView == player)
							SendPacket(visionPlayer);

				if (player.seerView == player ||
				    player.GetVehicle())
					SendPacket(player);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if (creature.GetExactDist2dSq(i_source) > i_distSq)
					continue;

				// Send packet to all who are sharing the creature's vision
				if (creature.HasSharedVision())
					foreach (var player in creature.GetSharedVisionList())
						if (player.seerView == creature)
							SendPacket(player);
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];

				if (!dynamicObject.InSamePhase(i_phaseShift))
					continue;

				if (dynamicObject.GetExactDist2dSq(i_source) > i_distSq)
					continue;

				Unit caster = dynamicObject.GetCaster();

				if (caster != null)
				{
					// Send packet back to the caster if the caster has vision of dynamic object
					Player player = caster.ToPlayer();

					if (player && player.seerView == dynamicObject)
						SendPacket(player);
				}
			}
		}

		private void SendPacket(Player player)
		{
			// never send packet to self
			if (player == i_source ||
			    !player.HaveAtClient(i_source) ||
			    player.IsFriendlyTo(i_source))
				return;

			i_packetSender.Invoke(player);
		}
	}

	public class UpdaterNotifier : Notifier
	{
		private uint i_timeDiff;

		public UpdaterNotifier(uint diff)
		{
			i_timeDiff = diff;
		}

		public override void Visit(IList<WorldObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				WorldObject obj = objs[i];

				if (obj.IsTypeId(TypeId.Player) ||
				    obj.IsTypeId(TypeId.Corpse))
					continue;

				if (obj.IsInWorld)
					obj.Update(i_timeDiff);
			}
		}
	}

	public class PlayerWorker : Notifier
	{
		private Action<Player> action;
		private PhaseShift i_phaseShift;

		public PlayerWorker(WorldObject searcher, Action<Player> _action)
		{
			i_phaseShift = searcher.GetPhaseShift();
			action       = _action;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (player.InSamePhase(i_phaseShift))
					action.Invoke(player);
			}
		}
	}

	public class CreatureWorker : Notifier
	{
		private IDoWork<Creature> Do;
		private PhaseShift i_phaseShift;

		public CreatureWorker(WorldObject searcher, IDoWork<Creature> _Do)
		{
			i_phaseShift = searcher.GetPhaseShift();
			Do           = _Do;
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (creature.InSamePhase(i_phaseShift))
					Do.Invoke(creature);
			}
		}
	}

	public class GameObjectWorker : Notifier
	{
		private IDoWork<GameObject> _do;
		private PhaseShift i_phaseShift;

		public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> @do)
		{
			i_phaseShift = searcher.GetPhaseShift();
			_do          = @do;
		}

		public override void Visit(IList<GameObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (gameObject.InSamePhase(i_phaseShift))
					_do.Invoke(gameObject);
			}
		}
	}

	public class WorldObjectWorker : Notifier
	{
		private IDoWork<WorldObject> i_do;
		private GridMapTypeMask i_mapTypeMask;
		private PhaseShift i_phaseShift;

		public WorldObjectWorker(WorldObject searcher, IDoWork<WorldObject> _do, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
		{
			i_mapTypeMask = mapTypeMask;
			i_phaseShift  = searcher.GetPhaseShift();
			i_do          = _do;
		}

		public override void Visit(IList<GameObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (gameObject.InSamePhase(i_phaseShift))
					i_do.Invoke(gameObject);
			}
		}

		public override void Visit(IList<Player> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (player.InSamePhase(i_phaseShift))
					i_do.Invoke(player);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (creature.InSamePhase(i_phaseShift))
					i_do.Invoke(creature);
			}
		}

		public override void Visit(IList<Corpse> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Corpse corpse = objs[i];

				if (corpse.InSamePhase(i_phaseShift))
					i_do.Invoke(corpse);
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];

				if (dynamicObject.InSamePhase(i_phaseShift))
					i_do.Invoke(dynamicObject);
			}
		}

		public override void Visit(IList<AreaTrigger> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				AreaTrigger areaTrigger = objs[i];

				if (areaTrigger.InSamePhase(i_phaseShift))
					i_do.Invoke(areaTrigger);
			}
		}

		public override void Visit(IList<SceneObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.SceneObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				SceneObject sceneObject = objs[i];

				if (sceneObject.InSamePhase(i_phaseShift))
					i_do.Invoke(sceneObject);
			}
		}

		public override void Visit(IList<Conversation> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Conversation conversation = objs[i];

				if (conversation.InSamePhase(i_phaseShift))
					i_do.Invoke(conversation);
			}
		}
	}

	public class ResetNotifier : Notifier
	{
		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];
				player.ResetAllNotifies();
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];
				creature.ResetAllNotifies();
			}
		}
	}

	public class WorldObjectChangeAccumulator : Notifier
	{
		private List<ObjectGuid> plr_list = new();

		private Dictionary<Player, UpdateData> updateData;
		private WorldObject worldObject;

		public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d)
		{
			updateData  = d;
			worldObject = obj;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];
				BuildPacket(player);

				if (!player.GetSharedVisionList().Empty())
					foreach (var visionPlayer in player.GetSharedVisionList())
						BuildPacket(visionPlayer);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.GetSharedVisionList().Empty())
					foreach (var visionPlayer in creature.GetSharedVisionList())
						BuildPacket(visionPlayer);
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];

				ObjectGuid guid = dynamicObject.GetCasterGUID();

				if (guid.IsPlayer())
				{
					//Caster may be NULL if DynObj is in removelist
					Player caster = Global.ObjAccessor.FindPlayer(guid);

					if (caster != null)
						if (caster._activePlayerData.FarsightObject == dynamicObject.GetGUID())
							BuildPacket(caster);
				}
			}
		}

		private void BuildPacket(Player player)
		{
			// Only send update once to a player
			if (!plr_list.Contains(player.GetGUID()) &&
			    player.HaveAtClient(worldObject))
			{
				worldObject.BuildFieldsUpdate(player, updateData);
				plr_list.Add(player.GetGUID());
			}
		}
	}

	public class PlayerDistWorker : Notifier
	{
		private IDoWork<Player> _do;
		private float i_dist;
		private WorldObject i_searcher;

		public PlayerDistWorker(WorldObject searcher, float _dist, IDoWork<Player> @do)
		{
			i_searcher = searcher;
			i_dist     = _dist;
			_do        = @do;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (player.InSamePhase(i_searcher) &&
				    player.IsWithinDist(i_searcher, i_dist))
					_do.Invoke(player);
			}
		}
	}

	public class CallOfHelpCreatureInRangeDo : IDoWork<Creature>
	{
		private Unit i_enemy;

		private Unit i_funit;
		private float i_range;

		public CallOfHelpCreatureInRangeDo(Unit funit, Unit enemy, float range)
		{
			i_funit = funit;
			i_enemy = enemy;
			i_range = range;
		}

		public void Invoke(Creature u)
		{
			if (u == i_funit)
				return;

			if (!u.CanAssistTo(i_funit, i_enemy, false))
				return;

			// too far
			// Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
			if (!u.IsWithinDist(i_funit, i_range, true, false, false))
				return;

			// only if see assisted creature's enemy
			if (!u.IsWithinLOSInMap(i_enemy))
				return;

			u.EngageWithTarget(i_enemy);
		}
	}

	public class LocalizedDo : IDoWork<Player>
	{
		private IDoWork<Player>[] _localizedCache = new IDoWork<Player>[(int)Locale.Total]; // 0 = default, i => i-1 locale index

		private MessageBuilder _localizer;

		public LocalizedDo(MessageBuilder localizer)
		{
			_localizer = localizer;
		}

		public void Invoke(Player player)
		{
			Locale          loc_idx   = player.GetSession().GetSessionDbLocaleIndex();
			int             cache_idx = (int)loc_idx + 1;
			IDoWork<Player> action;

			// create if not cached yet
			if (_localizedCache.Length < cache_idx + 1 ||
			    _localizedCache[cache_idx] == null)
			{
				if (_localizedCache.Length < cache_idx + 1)
					Array.Resize(ref _localizedCache, cache_idx + 1);

				action                     = _localizer.Invoke(loc_idx);
				_localizedCache[cache_idx] = action;
			}
			else
			{
				action = _localizedCache[cache_idx];
			}

			action.Invoke(player);
		}
	}

	public class RespawnDo : IDoWork<WorldObject>
	{
		public void Invoke(WorldObject obj)
		{
			switch (obj.GetTypeId())
			{
				case TypeId.Unit:
					obj.ToCreature().Respawn();

					break;
				case TypeId.GameObject:
					obj.ToGameObject().Respawn();

					break;
			}
		}
	}

	//Searchers
	public class WorldObjectSearcher : Notifier
	{
		private ICheck<WorldObject> i_check;
		private GridMapTypeMask i_mapTypeMask;
		private WorldObject i_object;
		private PhaseShift i_phaseShift;

		public WorldObjectSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
		{
			i_mapTypeMask = mapTypeMask;
			i_phaseShift  = searcher.GetPhaseShift();
			i_check       = check;
		}

		public override void Visit(IList<GameObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (!gameObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(gameObject))
				{
					i_object = gameObject;

					return;
				}
			}
		}

		public override void Visit(IList<Player> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(player))
				{
					i_object = player;

					return;
				}
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(creature))
				{
					i_object = creature;

					return;
				}
			}
		}

		public override void Visit(IList<Corpse> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Corpse corpse = objs[i];

				if (!corpse.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(corpse))
				{
					i_object = corpse;

					return;
				}
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];

				if (!dynamicObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(dynamicObject))
				{
					i_object = dynamicObject;

					return;
				}
			}
		}

		public override void Visit(IList<AreaTrigger> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				AreaTrigger areaTrigger = objs[i];

				if (!areaTrigger.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(areaTrigger))
				{
					i_object = areaTrigger;

					return;
				}
			}
		}

		public override void Visit(IList<SceneObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.SceneObject))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				SceneObject sceneObject = objs[i];

				if (!sceneObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(sceneObject))
				{
					i_object = sceneObject;

					return;
				}
			}
		}

		public override void Visit(IList<Conversation> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
				return;

			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Conversation conversation = objs[i];

				if (!conversation.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(conversation))
				{
					i_object = conversation;

					return;
				}
			}
		}

		public WorldObject GetTarget()
		{
			return i_object;
		}
	}

	public class WorldObjectLastSearcher : Notifier
	{
		private ICheck<WorldObject> i_check;
		private GridMapTypeMask i_mapTypeMask;
		private WorldObject i_object;
		private PhaseShift i_phaseShift;

		public WorldObjectLastSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
		{
			i_mapTypeMask = mapTypeMask;
			i_phaseShift  = searcher.GetPhaseShift();
			i_check       = check;
		}

		public override void Visit(IList<GameObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (!gameObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(gameObject))
					i_object = gameObject;
			}
		}

		public override void Visit(IList<Player> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(player))
					i_object = player;
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(creature))
					i_object = creature;
			}
		}

		public override void Visit(IList<Corpse> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Corpse corpse = objs[i];

				if (!corpse.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(corpse))
					i_object = corpse;
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];

				if (!dynamicObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(dynamicObject))
					i_object = dynamicObject;
			}
		}

		public override void Visit(IList<AreaTrigger> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				AreaTrigger areaTrigger = objs[i];

				if (!areaTrigger.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(areaTrigger))
					i_object = areaTrigger;
			}
		}

		public override void Visit(IList<SceneObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.SceneObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				SceneObject sceneObject = objs[i];

				if (!sceneObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(sceneObject))
					i_object = sceneObject;
			}
		}

		public override void Visit(IList<Conversation> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Conversation conversation = objs[i];

				if (!conversation.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(conversation))
					i_object = conversation;
			}
		}

		public WorldObject GetTarget()
		{
			return i_object;
		}
	}

	public class WorldObjectListSearcher : Notifier
	{
		private ICheck<WorldObject> i_check;
		private GridMapTypeMask i_mapTypeMask;
		private List<WorldObject> i_objects;
		private PhaseShift i_phaseShift;

		public WorldObjectListSearcher(WorldObject searcher, List<WorldObject> objects, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
		{
			i_mapTypeMask = mapTypeMask;
			i_phaseShift  = searcher.GetPhaseShift();
			i_objects     = objects;
			i_check       = check;
		}

		public override void Visit(IList<Player> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (i_check.Invoke(player))
					i_objects.Add(player);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (i_check.Invoke(creature))
					i_objects.Add(creature);
			}
		}

		public override void Visit(IList<Corpse> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Corpse corpse = objs[i];

				if (i_check.Invoke(corpse))
					i_objects.Add(corpse);
			}
		}

		public override void Visit(IList<GameObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (i_check.Invoke(gameObject))
					i_objects.Add(gameObject);
			}
		}

		public override void Visit(IList<DynamicObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				DynamicObject dynamicObject = objs[i];

				if (i_check.Invoke(dynamicObject))
					i_objects.Add(dynamicObject);
			}
		}

		public override void Visit(IList<AreaTrigger> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				AreaTrigger areaTrigger = objs[i];

				if (i_check.Invoke(areaTrigger))
					i_objects.Add(areaTrigger);
			}
		}

		public override void Visit(IList<SceneObject> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				SceneObject sceneObject = objs[i];

				if (i_check.Invoke(sceneObject))
					i_objects.Add(sceneObject);
			}
		}

		public override void Visit(IList<Conversation> objs)
		{
			if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Conversation conversation = objs[i];

				if (i_check.Invoke(conversation))
					i_objects.Add(conversation);
			}
		}
	}

	public class GameObjectSearcher : Notifier
	{
		private ICheck<GameObject> i_check;
		private GameObject i_object;
		private PhaseShift i_phaseShift;

		public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<GameObject> objs)
		{
			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (!gameObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(gameObject))
				{
					i_object = gameObject;

					return;
				}
			}
		}

		public GameObject GetTarget()
		{
			return i_object;
		}
	}

	public class GameObjectLastSearcher : Notifier
	{
		private ICheck<GameObject> i_check;
		private GameObject i_object;
		private PhaseShift i_phaseShift;

		public GameObjectLastSearcher(WorldObject searcher, ICheck<GameObject> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<GameObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (!gameObject.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(gameObject))
					i_object = gameObject;
			}
		}

		public GameObject GetTarget()
		{
			return i_object;
		}
	}

	public class GameObjectListSearcher : Notifier
	{
		private ICheck<GameObject> i_check;
		private List<GameObject> i_objects;
		private PhaseShift i_phaseShift;

		public GameObjectListSearcher(WorldObject searcher, List<GameObject> objects, ICheck<GameObject> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_objects    = objects;
			i_check      = check;
		}

		public override void Visit(IList<GameObject> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				GameObject gameObject = objs[i];

				if (gameObject.InSamePhase(i_phaseShift))
					if (i_check.Invoke(gameObject))
						i_objects.Add(gameObject);
			}
		}
	}

	public class UnitSearcher : Notifier
	{
		private ICheck<Unit> i_check;
		private Unit i_object;
		private PhaseShift i_phaseShift;

		public UnitSearcher(WorldObject searcher, ICheck<Unit> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(player))
				{
					i_object = player;

					return;
				}
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(creature))
				{
					i_object = creature;

					return;
				}
			}
		}

		public Unit GetTarget()
		{
			return i_object;
		}
	}

	public class UnitLastSearcher : Notifier
	{
		private ICheck<Unit> i_check;
		private Unit i_object;
		private PhaseShift i_phaseShift;

		public UnitLastSearcher(WorldObject searcher, ICheck<Unit> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(player))
					i_object = player;
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(creature))
					i_object = creature;
			}
		}

		public Unit GetTarget()
		{
			return i_object;
		}
	}

	public class UnitListSearcher : Notifier
	{
		private ICheck<Unit> i_check;
		private List<Unit> i_objects;
		private PhaseShift i_phaseShift;

		public UnitListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Unit> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_objects    = objects;
			i_check      = check;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (player.InSamePhase(i_phaseShift))
					if (i_check.Invoke(player))
						i_objects.Add(player);
			}
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (creature.InSamePhase(i_phaseShift))
					if (i_check.Invoke(creature))
						i_objects.Add(creature);
			}
		}
	}

	public class CreatureSearcher : Notifier
	{
		private ICheck<Creature> i_check;
		private Creature i_object;
		private PhaseShift i_phaseShift;

		public CreatureSearcher(WorldObject searcher, ICheck<Creature> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<Creature> objs)
		{
			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(creature))
				{
					i_object = creature;

					return;
				}
			}
		}

		public Creature GetTarget()
		{
			return i_object;
		}
	}

	public class CreatureLastSearcher : Notifier
	{
		private ICheck<Creature> i_check;
		private Creature i_object;
		internal PhaseShift i_phaseShift;

		public CreatureLastSearcher(WorldObject searcher, ICheck<Creature> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (!creature.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(creature))
					i_object = creature;
			}
		}

		public Creature GetTarget()
		{
			return i_object;
		}
	}

	public class CreatureListSearcher : Notifier
	{
		private ICheck<Creature> i_check;
		private List<Creature> i_objects;
		internal PhaseShift i_phaseShift;

		public CreatureListSearcher(WorldObject searcher, List<Creature> objects, ICheck<Creature> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_objects    = objects;
			i_check      = check;
		}

		public override void Visit(IList<Creature> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Creature creature = objs[i];

				if (creature.InSamePhase(i_phaseShift))
					if (i_check.Invoke(creature))
						i_objects.Add(creature);
			}
		}
	}

	public class PlayerSearcher : Notifier
	{
		private ICheck<Player> i_check;
		private Player i_object;
		private PhaseShift i_phaseShift;

		public PlayerSearcher(WorldObject searcher, ICheck<Player> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<Player> objs)
		{
			// already found
			if (i_object)
				return;

			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(player))
				{
					i_object = player;

					return;
				}
			}
		}

		public Player GetTarget()
		{
			return i_object;
		}
	}

	public class PlayerLastSearcher : Notifier
	{
		private ICheck<Player> i_check;
		private Player i_object;
		private PhaseShift i_phaseShift;

		public PlayerLastSearcher(WorldObject searcher, ICheck<Player> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_check      = check;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (!player.InSamePhase(i_phaseShift))
					continue;

				if (i_check.Invoke(player))
					i_object = player;
			}
		}

		public Player GetTarget()
		{
			return i_object;
		}
	}

	public class PlayerListSearcher : Notifier
	{
		private ICheck<Player> i_check;
		private List<Unit> i_objects;
		private PhaseShift i_phaseShift;

		public PlayerListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Player> check)
		{
			i_phaseShift = searcher.GetPhaseShift();
			i_objects    = objects;
			i_check      = check;
		}

		public PlayerListSearcher(PhaseShift phaseShift, List<Unit> objects, ICheck<Player> check)
		{
			i_phaseShift = phaseShift;
			i_objects    = objects;
			i_check      = check;
		}

		public override void Visit(IList<Player> objs)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				Player player = objs[i];

				if (player.InSamePhase(i_phaseShift))
					if (i_check.Invoke(player))
						i_objects.Add(player);
			}
		}
	}

	//Checks

	#region Checks

	public class MostHPMissingInRange<T> : ICheck<T> where T : Unit
	{
		private ulong i_hp;

		private Unit i_obj;
		private float i_range;

		public MostHPMissingInRange(Unit obj, float range, uint hp)
		{
			i_obj   = obj;
			i_range = range;
			i_hp    = hp;
		}

		public bool Invoke(T u)
		{
			if (u.IsAlive() &&
			    u.IsInCombat() &&
			    !i_obj.IsHostileTo(u) &&
			    i_obj.IsWithinDist(u, i_range) &&
			    u.GetMaxHealth() - u.GetHealth() > i_hp)
			{
				i_hp = (uint)(u.GetMaxHealth() - u.GetHealth());

				return true;
			}

			return false;
		}
	}

	internal class MostHPPercentMissingInRange : ICheck<Unit>
	{
		private float _hpPct;
		private float _maxHpPct;
		private float _minHpPct;
		private Unit _obj;
		private float _range;

		public MostHPPercentMissingInRange(Unit obj, float range, uint minHpPct, uint maxHpPct)
		{
			_obj      = obj;
			_range    = range;
			_minHpPct = minHpPct;
			_maxHpPct = maxHpPct;
			_hpPct    = 101.0f;
		}

		public bool Invoke(Unit u)
		{
			if (u.IsAlive() &&
			    u.IsInCombat() &&
			    !_obj.IsHostileTo(u) &&
			    _obj.IsWithinDist(u, _range) &&
			    _minHpPct <= u.GetHealthPct() &&
			    u.GetHealthPct() <= _maxHpPct &&
			    u.GetHealthPct() < _hpPct)
			{
				_hpPct = u.GetHealthPct();

				return true;
			}

			return false;
		}
	}

	public class FriendlyBelowHpPctEntryInRange : ICheck<Unit>
	{
		private uint i_entry;
		private bool i_excludeSelf;

		private Unit i_obj;
		private byte i_pct;
		private float i_range;

		public FriendlyBelowHpPctEntryInRange(Unit obj, uint entry, float range, byte pct, bool excludeSelf)
		{
			i_obj         = obj;
			i_entry       = entry;
			i_range       = range;
			i_pct         = pct;
			i_excludeSelf = excludeSelf;
		}

		public bool Invoke(Unit u)
		{
			if (i_excludeSelf && i_obj.GetGUID() == u.GetGUID())
				return false;

			if (u.GetEntry() == i_entry &&
			    u.IsAlive() &&
			    u.IsInCombat() &&
			    !i_obj.IsHostileTo(u) &&
			    i_obj.IsWithinDist(u, i_range) &&
			    u.HealthBelowPct(i_pct))
				return true;

			return false;
		}
	}

	public class FriendlyCCedInRange : ICheck<Creature>
	{
		private Unit i_obj;
		private float i_range;

		public FriendlyCCedInRange(Unit obj, float range)
		{
			i_obj   = obj;
			i_range = range;
		}

		public bool Invoke(Creature u)
		{
			if (u.IsAlive() &&
			    u.IsInCombat() &&
			    !i_obj.IsHostileTo(u) &&
			    i_obj.IsWithinDist(u, i_range) &&
			    (u.IsFeared() || u.IsCharmed() || u.HasRootAura() || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused)))
				return true;

			return false;
		}
	}

	public class FriendlyMissingBuffInRange : ICheck<Creature>
	{
		private Unit i_obj;
		private float i_range;
		private uint i_spell;

		public FriendlyMissingBuffInRange(Unit obj, float range, uint spellid)
		{
			i_obj   = obj;
			i_range = range;
			i_spell = spellid;
		}

		public bool Invoke(Creature u)
		{
			if (u.IsAlive() &&
			    u.IsInCombat() &&
			    !i_obj.IsHostileTo(u) &&
			    i_obj.IsWithinDist(u, i_range) &&
			    !(u.HasAura(i_spell)))
				return true;

			return false;
		}
	}

	public class AnyUnfriendlyUnitInObjectRangeCheck : ICheck<Unit>
	{
		private Unit i_funit;

		private WorldObject i_obj;
		private float i_range;

		public AnyUnfriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
		{
			i_obj   = obj;
			i_funit = funit;
			i_range = range;
		}

		public bool Invoke(Unit u)
		{
			if (u.IsAlive() &&
			    i_obj.IsWithinDist(u, i_range) &&
			    !i_funit.IsFriendlyTo(u))
				return true;
			else
				return false;
		}
	}

	public class NearestAttackableNoTotemUnitInObjectRangeCheck : ICheck<Unit>
	{
		private WorldObject i_obj;
		private float i_range;

		public NearestAttackableNoTotemUnitInObjectRangeCheck(WorldObject obj, float range)
		{
			i_obj   = obj;
			i_range = range;
		}

		public bool Invoke(Unit u)
		{
			if (!u.IsAlive())
				return false;

			if (u.GetCreatureType() == CreatureType.NonCombatPet)
				return false;

			if (u.IsTypeId(TypeId.Unit) &&
			    u.IsTotem())
				return false;

			if (!u.IsTargetableForAttack(false))
				return false;

			if (!i_obj.IsWithinDist(u, i_range) ||
			    i_obj.IsValidAttackTarget(u))
				return false;

			i_range = i_obj.GetDistance(u);

			return true;
		}
	}

	public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
	{
		private Unit i_funit;
		private bool i_incOwnRadius;
		private bool i_incTargetRadius;

		private WorldObject i_obj;
		private bool i_playerOnly;
		private float i_range;

		public AnyFriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
		{
			i_obj             = obj;
			i_funit           = funit;
			i_range           = range;
			i_playerOnly      = playerOnly;
			i_incOwnRadius    = incOwnRadius;
			i_incTargetRadius = incTargetRadius;
		}

		public bool Invoke(Unit u)
		{
			if (!u.IsAlive())
				return false;

			float searchRadius = i_range;

			if (i_incOwnRadius)
				searchRadius += i_obj.GetCombatReach();

			if (i_incTargetRadius)
				searchRadius += u.GetCombatReach();

			if (!u.IsInMap(i_obj) ||
			    !u.InSamePhase(i_obj) ||
			    !u.IsWithinDoubleVerticalCylinder(i_obj, searchRadius, searchRadius))
				return false;

			if (!i_funit.IsFriendlyTo(u))
				return false;

			return !i_playerOnly || u.GetTypeId() == TypeId.Player;
		}
	}

	public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
	{
		private bool _playerOnly;
		private bool _raid;
		private float _range;
		private Unit _refUnit;

		private WorldObject _source;
		private bool i_incOwnRadius;
		private bool i_incTargetRadius;

		public AnyGroupedUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool raid, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
		{
			_source           = obj;
			_refUnit          = funit;
			_range            = range;
			_raid             = raid;
			_playerOnly       = playerOnly;
			i_incOwnRadius    = incOwnRadius;
			i_incTargetRadius = incTargetRadius;
		}

		public bool Invoke(Unit u)
		{
			if (_playerOnly && !u.IsPlayer())
				return false;

			if (_raid)
			{
				if (!_refUnit.IsInRaidWith(u))
					return false;
			}
			else if (!_refUnit.IsInPartyWith(u))
			{
				return false;
			}

			if (_refUnit.IsHostileTo(u))
				return false;

			if (!u.IsAlive())
				return false;

			float searchRadius = _range;

			if (i_incOwnRadius)
				searchRadius += _source.GetCombatReach();

			if (i_incTargetRadius)
				searchRadius += u.GetCombatReach();

			return u.IsInMap(_source) && u.InSamePhase(_source) && u.IsWithinDoubleVerticalCylinder(_source, searchRadius, searchRadius);
		}
	}

	public class AnyUnitInObjectRangeCheck : ICheck<Unit>
	{
		private bool i_check3D;

		private WorldObject i_obj;
		private float i_range;

		public AnyUnitInObjectRangeCheck(WorldObject obj, float range, bool check3D = true)
		{
			i_obj     = obj;
			i_range   = range;
			i_check3D = check3D;
		}

		public bool Invoke(Unit u)
		{
			if (u.IsAlive() &&
			    i_obj.IsWithinDist(u, i_range, i_check3D))
				return true;

			return false;
		}
	}

	// Success at unit in range, range update for next check (this can be use with UnitLastSearcher to find nearest unit)
	public class NearestAttackableUnitInObjectRangeCheck : ICheck<Unit>
	{
		private Unit i_funit;

		private WorldObject i_obj;
		private float i_range;

		public NearestAttackableUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
		{
			i_obj   = obj;
			i_funit = funit;
			i_range = range;
		}

		public bool Invoke(Unit u)
		{
			if (u.IsTargetableForAttack() &&
			    i_obj.IsWithinDist(u, i_range) &&
			    (i_funit.IsInCombatWith(u) || i_funit.IsHostileTo(u)) &&
			    i_obj.CanSeeOrDetect(u))
			{
				i_range = i_obj.GetDistance(u); // use found unit range as new range limit for next check

				return true;
			}

			return false;
		}
	}

	public class AnyAoETargetUnitInObjectRangeCheck : ICheck<Unit>
	{
		private SpellInfo _spellInfo;
		private Unit i_funit;
		private bool i_incOwnRadius;
		private bool i_incTargetRadius;

		private WorldObject i_obj;
		private float i_range;

		public AnyAoETargetUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, SpellInfo spellInfo = null, bool incOwnRadius = true, bool incTargetRadius = true)
		{
			i_obj             = obj;
			i_funit           = funit;
			_spellInfo        = spellInfo;
			i_range           = range;
			i_incOwnRadius    = incOwnRadius;
			i_incTargetRadius = incTargetRadius;
		}

		public bool Invoke(Unit u)
		{
			// Check contains checks for: live, uninteractible, non-attackable flags, flight check and GM check, ignore totems
			if (u.IsTypeId(TypeId.Unit) &&
			    u.IsTotem())
				return false;

			if (_spellInfo != null)
			{
				if (!u.IsPlayer())
				{
					if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
						return false;

					if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) &&
					    u.IsControlledByPlayer())
						return false;
				}
				else if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
				{
					return false;
				}
			}

			if (!i_funit.IsValidAttackTarget(u, _spellInfo))
				return false;

			float searchRadius = i_range;

			if (i_incOwnRadius)
				searchRadius += i_obj.GetCombatReach();

			if (i_incTargetRadius)
				searchRadius += u.GetCombatReach();

			return u.IsInMap(i_obj) && u.InSamePhase(i_obj) && u.IsWithinDoubleVerticalCylinder(i_obj, searchRadius, searchRadius);
		}
	}

	public class AnyDeadUnitCheck : ICheck<Unit>
	{
		public bool Invoke(Unit u)
		{
			return !u.IsAlive();
		}
	}

	public class NearestHostileUnitCheck : ICheck<Unit>
	{
		private float _range;
		private bool i_playerOnly;

		private Creature me;

		public NearestHostileUnitCheck(Creature creature, float dist = 0, bool playerOnly = false)
		{
			me           = creature;
			i_playerOnly = playerOnly;

			_range = (dist == 0 ? 9999 : dist);
		}

		public bool Invoke(Unit u)
		{
			if (!me.IsWithinDist(u, _range))
				return false;

			if (!me.IsValidAttackTarget(u))
				return false;

			if (i_playerOnly && !u.IsTypeId(TypeId.Player))
				return false;

			_range = me.GetDistance(u); // use found unit range as new range limit for next check

			return true;
		}
	}

	internal class NearestHostileUnitInAttackDistanceCheck : ICheck<Unit>
	{
		private bool _force;
		private float _range;

		private Creature me;

		public NearestHostileUnitInAttackDistanceCheck(Creature creature, float dist = 0)
		{
			me     = creature;
			_range = (dist == 0 ? 9999 : dist);
			_force = (dist != 0);
		}

		public bool Invoke(Unit u)
		{
			if (!me.IsWithinDist(u, _range))
				return false;

			if (!me.CanSeeOrDetect(u))
				return false;

			if (_force)
			{
				if (!me.IsValidAttackTarget(u))
					return false;
			}
			else if (!me.CanStartAttack(u, false))
			{
				return false;
			}

			_range = me.GetDistance(u); // use found unit range as new range limit for next check

			return true;
		}
	}

	internal class NearestHostileUnitInAggroRangeCheck : ICheck<Unit>
	{
		private bool _ignoreCivilians;

		private Creature _me;
		private bool _useLOS;

		public NearestHostileUnitInAggroRangeCheck(Creature creature, bool useLOS = false, bool ignoreCivilians = false)
		{
			_me              = creature;
			_useLOS          = useLOS;
			_ignoreCivilians = ignoreCivilians;
		}

		public bool Invoke(Unit u)
		{
			if (!u.IsHostileTo(_me))
				return false;

			if (!u.IsWithinDist(_me, _me.GetAggroRange(u)))
				return false;

			if (!_me.IsValidAttackTarget(u))
				return false;

			if (_useLOS && !u.IsWithinLOSInMap(_me))
				return false;

			// pets in aggressive do not attack civilians
			if (_ignoreCivilians)
			{
				Creature c = u.ToCreature();

				if (c != null)
					if (c.IsCivilian())
						return false;
			}

			return true;
		}
	}

	internal class AnyAssistCreatureInRangeCheck : ICheck<Creature>
	{
		private Unit i_enemy;

		private Unit i_funit;
		private float i_range;

		public AnyAssistCreatureInRangeCheck(Unit funit, Unit enemy, float range)
		{
			i_funit = funit;
			i_enemy = enemy;
			i_range = range;
		}

		public bool Invoke(Creature u)
		{
			if (u == i_funit)
				return false;

			if (!u.CanAssistTo(i_funit, i_enemy))
				return false;

			// too far
			// Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
			if (!i_funit.IsWithinDist(u, i_range, true, false, false))
				return false;

			// only if see assisted creature
			if (!i_funit.IsWithinLOSInMap(u))
				return false;

			return true;
		}
	}

	internal class NearestAssistCreatureInCreatureRangeCheck : ICheck<Creature>
	{
		private Unit i_enemy;

		private Creature i_obj;
		private float i_range;

		public NearestAssistCreatureInCreatureRangeCheck(Creature obj, Unit enemy, float range)
		{
			i_obj   = obj;
			i_enemy = enemy;
			i_range = range;
		}

		public bool Invoke(Creature u)
		{
			if (u == i_obj)
				return false;

			if (!u.CanAssistTo(i_obj, i_enemy))
				return false;

			// Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
			if (!i_obj.IsWithinDist(u, i_range, true, false, false))
				return false;

			if (!i_obj.IsWithinLOSInMap(u))
				return false;

			i_range = i_obj.GetDistance(u); // use found unit range as new range limit for next check

			return true;
		}
	}

	// Success at unit in range, range update for next check (this can be use with CreatureLastSearcher to find nearest creature)
	internal class NearestCreatureEntryWithLiveStateInObjectRangeCheck : ICheck<Creature>
	{
		private bool i_alive;
		private uint i_entry;

		private WorldObject i_obj;
		private float i_range;

		public NearestCreatureEntryWithLiveStateInObjectRangeCheck(WorldObject obj, uint entry, bool alive, float range)
		{
			i_obj   = obj;
			i_entry = entry;
			i_alive = alive;
			i_range = range;
		}

		public bool Invoke(Creature u)
		{
			if (u.GetDeathState() != DeathState.Dead &&
			    u.GetEntry() == i_entry &&
			    u.IsAlive() == i_alive &&
			    u.GetGUID() != i_obj.GetGUID() &&
			    i_obj.IsWithinDist(u, i_range) &&
			    u.CheckPrivateObjectOwnerVisibility(i_obj))
			{
				i_range = i_obj.GetDistance(u); // use found unit range as new range limit for next check

				return true;
			}

			return false;
		}
	}

	public class CreatureWithOptionsInObjectRangeCheck<T> : ICheck<Creature> where T : NoopCheckCustomizer
	{
		private FindCreatureOptions i_args;
		private T i_customizer;
		private WorldObject i_obj;

		public CreatureWithOptionsInObjectRangeCheck(WorldObject obj, T customizer, FindCreatureOptions args)
		{
			i_obj        = obj;
			i_args       = args;
			i_customizer = customizer;
		}

		public bool Invoke(Creature u)
		{
			if (u.GetDeathState() == DeathState.Dead) // Despawned
				return false;

			if (u.GetGUID() == i_obj.GetGUID())
				return false;

			if (!i_customizer.Test(u))
				return false;

			if (i_args.CreatureId.HasValue &&
			    u.GetEntry() != i_args.CreatureId)
				return false;

			if (i_args.StringId != null &&
			    !u.HasStringId(i_args.StringId))
				return false;

			if (i_args.IsAlive.HasValue &&
			    u.IsAlive() != i_args.IsAlive)
				return false;

			if (i_args.IsSummon.HasValue &&
			    u.IsSummon() != i_args.IsSummon)
				return false;

			if (i_args.IsInCombat.HasValue &&
			    u.IsInCombat() != i_args.IsInCombat)
				return false;

			if ((i_args.OwnerGuid.HasValue && u.GetOwnerGUID() != i_args.OwnerGuid) ||
			    (i_args.CharmerGuid.HasValue && u.GetCharmerGUID() != i_args.CharmerGuid) ||
			    (i_args.CreatorGuid.HasValue && u.GetCreatorGUID() != i_args.CreatorGuid) ||
			    (i_args.DemonCreatorGuid.HasValue && u.GetDemonCreatorGUID() != i_args.DemonCreatorGuid) ||
			    (i_args.PrivateObjectOwnerGuid.HasValue && u.GetPrivateObjectOwner() != i_args.PrivateObjectOwnerGuid))
				return false;

			if (i_args.IgnorePrivateObjects &&
			    u.IsPrivateObject())
				return false;

			if (i_args.IgnoreNotOwnedPrivateObjects &&
			    !u.CheckPrivateObjectOwnerVisibility(i_obj))
				return false;

			if (i_args.AuraSpellId.HasValue &&
			    !u.HasAura((uint)i_args.AuraSpellId))
				return false;

			i_customizer.Update(u);

			return true;
		}
	}

	public class AnyPlayerInObjectRangeCheck : ICheck<Player>
	{
		private WorldObject _obj;
		private float _range;
		private bool _reqAlive;

		public AnyPlayerInObjectRangeCheck(WorldObject obj, float range, bool reqAlive = true)
		{
			_obj      = obj;
			_range    = range;
			_reqAlive = reqAlive;
		}

		public bool Invoke(Player pl)
		{
			if (_reqAlive && !pl.IsAlive())
				return false;

			if (!_obj.IsWithinDist(pl, _range))
				return false;

			return true;
		}
	}

	internal class AnyPlayerInPositionRangeCheck : ICheck<Player>
	{
		private Position _pos;
		private float _range;
		private bool _reqAlive;

		public AnyPlayerInPositionRangeCheck(Position pos, float range, bool reqAlive = true)
		{
			_pos      = pos;
			_range    = range;
			_reqAlive = reqAlive;
		}

		public bool Invoke(Player u)
		{
			if (_reqAlive && !u.IsAlive())
				return false;

			if (!u.IsWithinDist3d(_pos, _range))
				return false;

			return true;
		}
	}

	internal class NearestPlayerInObjectRangeCheck : ICheck<Player>
	{
		private WorldObject i_obj;
		private float i_range;

		public NearestPlayerInObjectRangeCheck(WorldObject obj, float range)
		{
			i_obj   = obj;
			i_range = range;
		}

		public bool Invoke(Player pl)
		{
			if (pl.IsAlive() &&
			    i_obj.IsWithinDist(pl, i_range))
			{
				i_range = i_obj.GetDistance(pl);

				return true;
			}

			return false;
		}
	}

	internal class AllFriendlyCreaturesInGrid : ICheck<Unit>
	{
		private Unit unit;

		public AllFriendlyCreaturesInGrid(Unit obj)
		{
			unit = obj;
		}

		public bool Invoke(Unit u)
		{
			if (u.IsAlive() &&
			    u.IsVisible() &&
			    u.IsFriendlyTo(unit))
				return true;

			return false;
		}
	}

	internal class AllGameObjectsWithEntryInRange : ICheck<GameObject>
	{
		private float _fRange;

		private WorldObject _pObject;
		private uint _uiEntry;

		public AllGameObjectsWithEntryInRange(WorldObject obj, uint entry, float maxRange)
		{
			_pObject = obj;
			_uiEntry = entry;
			_fRange  = maxRange;
		}

		public bool Invoke(GameObject go)
		{
			if (_uiEntry == 0 ||
			    (go.GetEntry() == _uiEntry && _pObject.IsWithinDist(go, _fRange, false)))
				return true;

			return false;
		}
	}

	public class AllCreaturesOfEntryInRange : ICheck<Creature>
	{
		private float _fRange;

		private WorldObject _pObject;
		private uint _uiEntry;

		public AllCreaturesOfEntryInRange(WorldObject obj, uint entry, float maxRange = 0f)
		{
			_pObject = obj;
			_uiEntry = entry;
			_fRange  = maxRange;
		}

		public bool Invoke(Creature creature)
		{
			if (_uiEntry != 0)
				if (creature.GetEntry() != _uiEntry)
					return false;

			if (_fRange != 0f)
			{
				if (_fRange > 0.0f &&
				    !_pObject.IsWithinDist(creature, _fRange, false))
					return false;

				if (_fRange < 0.0f &&
				    _pObject.IsWithinDist(creature, _fRange, false))
					return false;
			}

			return true;
		}
	}

	internal class PlayerAtMinimumRangeAway : ICheck<Player>
	{
		private float fRange;

		private Unit unit;

		public PlayerAtMinimumRangeAway(Unit _unit, float fMinRange)
		{
			unit   = _unit;
			fRange = fMinRange;
		}

		public bool Invoke(Player player)
		{
			//No threat list check, must be done explicit if expected to be in combat with creature
			if (!player.IsGameMaster() &&
			    player.IsAlive() &&
			    !unit.IsWithinDist(player, fRange, false))
				return true;

			return false;
		}
	}

	internal class GameObjectInRangeCheck : ICheck<GameObject>
	{
		private uint entry;

		private float x, y, z, range;

		public GameObjectInRangeCheck(float _x, float _y, float _z, float _range, uint _entry = 0)
		{
			x     = _x;
			y     = _y;
			z     = _z;
			range = _range;
			entry = _entry;
		}

		public bool Invoke(GameObject go)
		{
			if (entry == 0 ||
			    (go.GetGoInfo() != null && go.GetGoInfo().entry == entry))
				return go.IsInRange(x, y, z, range);
			else return false;
		}
	}

	public class AllWorldObjectsInRange : ICheck<WorldObject>
	{
		private float _fRange;

		private WorldObject _pObject;

		public AllWorldObjectsInRange(WorldObject obj, float maxRange)
		{
			_pObject = obj;
			_fRange  = maxRange;
		}

		public bool Invoke(WorldObject go)
		{
			return _pObject.IsWithinDist(go, _fRange, false) && _pObject.InSamePhase(go);
		}
	}

	public class ObjectTypeIdCheck : ICheck<WorldObject>
	{
		private bool _equals;

		private TypeId _typeId;

		public ObjectTypeIdCheck(TypeId typeId, bool equals)
		{
			_typeId = typeId;
			_equals = equals;
		}

		public bool Invoke(WorldObject obj)
		{
			return (obj.GetTypeId() == _typeId) == _equals;
		}
	}

	public class ObjectGUIDCheck : ICheck<WorldObject>
	{
		private ObjectGuid _GUID;

		public ObjectGUIDCheck(ObjectGuid GUID)
		{
			_GUID = GUID;
		}

		public bool Invoke(WorldObject obj)
		{
			return obj.GetGUID() == _GUID;
		}

		public static implicit operator Predicate<WorldObject>(ObjectGUIDCheck check)
		{
			return check.Invoke;
		}
	}

	public class HeightDifferenceCheck : ICheck<WorldObject>
	{
		private WorldObject _baseObject;
		private float _difference;
		private bool _reverse;

		public HeightDifferenceCheck(WorldObject go, float diff, bool reverse)
		{
			_baseObject = go;
			_difference = diff;
			_reverse    = reverse;
		}

		public bool Invoke(WorldObject unit)
		{
			return (unit.GetPositionZ() - _baseObject.GetPositionZ() > _difference) != _reverse;
		}
	}

	public class UnitAuraCheck<T> : ICheck<T> where T : WorldObject
	{
		private ObjectGuid _casterGUID;

		private bool _present;
		private uint _spellId;

		public UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default)
		{
			_present    = present;
			_spellId    = spellId;
			_casterGUID = casterGUID;
		}

		public bool Invoke(T obj)
		{
			return obj.ToUnit() && obj.ToUnit().HasAura(_spellId, _casterGUID) == _present;
		}

		public static implicit operator Predicate<T>(UnitAuraCheck<T> unit)
		{
			return unit.Invoke;
		}
	}

	internal class ObjectEntryAndPrivateOwnerIfExistsCheck : ICheck<WorldObject>
	{
		private uint _entry;
		private ObjectGuid _ownerGUID;

		public ObjectEntryAndPrivateOwnerIfExistsCheck(ObjectGuid ownerGUID, uint entry)
		{
			_ownerGUID = ownerGUID;
			_entry     = entry;
		}

		public bool Invoke(WorldObject obj)
		{
			return obj.GetEntry() == _entry && (!obj.IsPrivateObject() || obj.GetPrivateObjectOwner() == _ownerGUID);
		}
	}

	internal class GameObjectFocusCheck : ICheck<GameObject>
	{
		private WorldObject _caster;
		private uint _focusId;

		public GameObjectFocusCheck(WorldObject caster, uint focusId)
		{
			_caster  = caster;
			_focusId = focusId;
		}

		public bool Invoke(GameObject go)
		{
			if (go.GetGoInfo().GetSpellFocusType() != _focusId)
				return false;

			if (!go.IsSpawned())
				return false;

			float dist = go.GetGoInfo().GetSpellFocusRadius();

			return go.IsWithinDist(_caster, dist);
		}
	}

	// Find the nearest Fishing hole and return true only if source object is in range of hole
	internal class NearestGameObjectFishingHole : ICheck<GameObject>
	{
		private WorldObject i_obj;
		private float i_range;

		public NearestGameObjectFishingHole(WorldObject obj, float range)
		{
			i_obj   = obj;
			i_range = range;
		}

		public bool Invoke(GameObject go)
		{
			if (go.GetGoInfo().type == GameObjectTypes.FishingHole &&
			    go.IsSpawned() &&
			    i_obj.IsWithinDist(go, i_range) &&
			    i_obj.IsWithinDist(go, go.GetGoInfo().FishingHole.radius))
			{
				i_range = i_obj.GetDistance(go);

				return true;
			}

			return false;
		}
	}

	internal class NearestGameObjectCheck : ICheck<GameObject>
	{
		private WorldObject i_obj;
		private float i_range;

		public NearestGameObjectCheck(WorldObject obj)
		{
			i_obj   = obj;
			i_range = 999;
		}

		public bool Invoke(GameObject go)
		{
			if (i_obj.IsWithinDist(go, i_range))
			{
				i_range = i_obj.GetDistance(go); // use found GO range as new range limit for next check

				return true;
			}

			return false;
		}
	}

	// Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO)
	internal class NearestGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
	{
		private uint _entry;

		private WorldObject _obj;
		private float _range;
		private bool _spawnedOnly;

		public NearestGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range, bool spawnedOnly = true)
		{
			_obj         = obj;
			_entry       = entry;
			_range       = range;
			_spawnedOnly = spawnedOnly;
		}

		public bool Invoke(GameObject go)
		{
			if ((!_spawnedOnly || go.IsSpawned()) &&
			    go.GetEntry() == _entry &&
			    go.GetGUID() != _obj.GetGUID() &&
			    _obj.IsWithinDist(go, _range))
			{
				_range = _obj.GetDistance(go); // use found GO range as new range limit for next check

				return true;
			}

			return false;
		}
	}

	// Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest unspawned GO)
	internal class NearestUnspawnedGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
	{
		private uint i_entry;
		private WorldObject i_obj;
		private float i_range;

		public NearestUnspawnedGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range)
		{
			i_obj   = obj;
			i_entry = entry;
			i_range = range;
		}

		public bool Invoke(GameObject go)
		{
			if (!go.IsSpawned() &&
			    go.GetEntry() == i_entry &&
			    go.GetGUID() != i_obj.GetGUID() &&
			    i_obj.IsWithinDist(go, i_range))
			{
				i_range = i_obj.GetDistance(go); // use found GO range as new range limit for next check

				return true;
			}

			return false;
		}
	}

	// Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO with a certain type)
	internal class NearestGameObjectTypeInObjectRangeCheck : ICheck<GameObject>
	{
		private WorldObject i_obj;
		private float i_range;
		private GameObjectTypes i_type;

		public NearestGameObjectTypeInObjectRangeCheck(WorldObject obj, GameObjectTypes type, float range)
		{
			i_obj   = obj;
			i_type  = type;
			i_range = range;
		}

		public bool Invoke(GameObject go)
		{
			if (go.GetGoType() == i_type &&
			    i_obj.IsWithinDist(go, i_range))
			{
				i_range = i_obj.GetDistance(go); // use found GO range as new range limit for next check

				return true;
			}

			return false;
		}
	}

	// CHECK modifiers
	public class NoopCheckCustomizer
	{
		public virtual bool Test(WorldObject o)
		{
			return true;
		}

		public virtual void Update(WorldObject o)
		{
		}
	}

	internal class NearestCheckCustomizer : NoopCheckCustomizer
	{
		private WorldObject i_obj;
		private float i_range;

		public NearestCheckCustomizer(WorldObject obj, float range)
		{
			i_obj   = obj;
			i_range = range;
		}

		public override bool Test(WorldObject o)
		{
			return i_obj.IsWithinDist(o, i_range);
		}

		public override void Update(WorldObject o)
		{
			i_range = i_obj.GetDistance(o);
		}
	}

	public class AnyDeadUnitObjectInRangeCheck<T> : ICheck<T> where T : WorldObject
	{
		private float i_range;

		private WorldObject i_searchObj;

		public AnyDeadUnitObjectInRangeCheck(WorldObject searchObj, float range)
		{
			i_searchObj = searchObj;
			i_range     = range;
		}

		public virtual bool Invoke(T obj)
		{
			Player player = obj.ToPlayer();

			if (player)
				return !player.IsAlive() && !player.HasAuraType(AuraType.Ghost) && i_searchObj.IsWithinDistInMap(player, i_range);

			Creature creature = obj.ToCreature();

			if (creature)
				return !creature.IsAlive() && i_searchObj.IsWithinDistInMap(creature, i_range);

			Corpse corpse = obj.ToCorpse();

			if (corpse)
				return corpse.GetCorpseType() != CorpseType.Bones && i_searchObj.IsWithinDistInMap(corpse, i_range);

			return false;
		}
	}

	public class AnyDeadUnitSpellTargetInRangeCheck<T> : AnyDeadUnitObjectInRangeCheck<T> where T : WorldObject
	{
		private WorldObjectSpellTargetCheck i_check;

		public AnyDeadUnitSpellTargetInRangeCheck(WorldObject searchObj, float range, SpellInfo spellInfo, SpellTargetCheckTypes check, SpellTargetObjectTypes objectType) : base(searchObj, range)
		{
			i_check = new WorldObjectSpellTargetCheck(searchObj, searchObj, spellInfo, check, null, objectType);
		}

		public override bool Invoke(T obj)
		{
			return base.Invoke(obj) && i_check.Invoke(obj);
		}
	}

	public class PlayerOrPetCheck : ICheck<WorldObject>
	{
		public bool Invoke(WorldObject obj)
		{
			if (obj.IsTypeId(TypeId.Player))
				return false;

			Creature creature = obj.ToCreature();

			if (creature)
				return !creature.IsPet();

			return true;
		}
	}

	#endregion
}