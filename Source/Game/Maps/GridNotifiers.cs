// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Bgs.Protocol.Notification.V1;
using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Maps.Interfaces;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Maps
{
    public static class NotifierHelpers
    {
        public static void CreatureUnitRelocationWorker(Creature c, Unit u)
        {
            if (!u.IsAlive() || !c.IsAlive() || c == u || u.IsInFlight())
                return;

            if (!c.HasUnitState(UnitState.Sightless))
            {
                if (c.IsAIEnabled() && c.CanSeeOrDetect(u, false, true))
                    c.GetAI().MoveInLineOfSight_Safe(u);
                else
                {
                    if (u.IsTypeId(TypeId.Player) && u.HasStealthAura() && c.IsAIEnabled() && c.CanSeeOrDetect(u, false, true, true))
                        c.GetAI().TriggerAlert(u);
                }
            }
        }
    }

    public class VisibleNotifier : IGridNotifierWorldObject
    {
        public GridType GridType { get; set; }
        public VisibleNotifier(Player pl, GridType gridType)
        {
            i_player = pl;
            i_data = new UpdateData(pl.GetMapId());
            vis_guids = new List<ObjectGuid>(pl.m_clientGUIDs);
            i_visibleNow = new List<Unit>();
            GridType = gridType;
        }

        public void Visit(IList<WorldObject> objs)
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
            {
                foreach (var obj in transport.GetPassengers())
                {
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
                }
            }

            foreach (var guid in vis_guids)
            {
                i_player.m_clientGUIDs.Remove(guid);
                i_data.AddOutOfRangeGUID(guid);

                if (guid.IsPlayer())
                {
                    Player pl = Global.ObjAccessor.FindPlayer(guid);
                    if (pl != null && pl.IsInWorld && !pl.IsNeedNotify(NotifyFlags.VisibilityChanged))
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

        internal Player i_player;
        internal UpdateData i_data;
        internal List<ObjectGuid> vis_guids;
        internal List<Unit> i_visibleNow;
    }

    public class VisibleChangesNotifier : IGridNotifierCreature, IGridNotifierPlayer, IGridNotifierDynamicObject
    {
        ICollection<WorldObject> i_objects;
        public GridType GridType { get; set; }

        public VisibleChangesNotifier(ICollection<WorldObject> objects, GridType gridType)
        {
            i_objects = objects;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                player.UpdateVisibilityOf(i_objects);

                foreach (var visionPlayer in player.GetSharedVisionList())
                {
                    if (visionPlayer.seerView == player)
                        visionPlayer.UpdateVisibilityOf(i_objects);
                }
            }
        }

        public void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                foreach (var visionPlayer in creature.GetSharedVisionList())
                    if (visionPlayer.seerView == creature)
                        visionPlayer.UpdateVisibilityOf(i_objects);
            }
        }

        public void Visit(IList<DynamicObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];
                Unit caster = dynamicObject.GetCaster();
                if (caster)
                {
                    Player pl = caster.ToPlayer();
                    if (pl && pl.seerView == dynamicObject)
                        pl.UpdateVisibilityOf(i_objects);
                }
            }
        }
    }

    public class PlayerRelocationNotifier : VisibleNotifier, IGridNotifierPlayer, IGridNotifierCreature
    {
        public PlayerRelocationNotifier(Player player, GridType gridType) : base(player, gridType) { }

        public void Visit(IList<Player> objs)
        {
            Visit(objs.Cast<WorldObject>().ToList());

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

        public void Visit(IList<Creature> objs)
        {
            Visit(objs.Cast<WorldObject>().ToList());

            bool relocated_for_ai = (i_player == i_player.seerView);

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                vis_guids.Remove(creature.GetGUID());

                i_player.UpdateVisibilityOf(creature, i_data, i_visibleNow);

                if (relocated_for_ai && !creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    NotifierHelpers.CreatureUnitRelocationWorker(creature, i_player);
            }
        }
    }

    public class CreatureRelocationNotifier : IGridNotifierCreature, IGridNotifierPlayer
    {
        public GridType GridType { get; set; }
        public CreatureRelocationNotifier(Creature c, GridType gridType)
        {
            i_creature = c;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                if (!player.seerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    player.UpdateVisibilityOf(i_creature);

                NotifierHelpers.CreatureUnitRelocationWorker(i_creature, player);
            }
        }

        public void Visit(IList<Creature> objs)
        {
            if (!i_creature.IsAlive())
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                NotifierHelpers.CreatureUnitRelocationWorker(i_creature, creature);

                if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    NotifierHelpers.CreatureUnitRelocationWorker(creature, i_creature);
            }
        }

        Creature i_creature;
    }

    public class DelayedUnitRelocation : IGridNotifierCreature, IGridNotifierPlayer
    {
        public GridType GridType { get; set; }
        public DelayedUnitRelocation(Cell c, CellCoord pair, Map map, float radius, GridType gridType)
        {
            i_map = map;
            cell = c;
            p = pair;
            i_radius = radius;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                WorldObject viewPoint = player.seerView;

                if (!viewPoint.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                if (player != viewPoint && !viewPoint.IsPositionValid())
                    continue;

                var relocate = new PlayerRelocationNotifier(player, GridType.All);
                Cell.VisitGrid(viewPoint, relocate, i_radius, false);

                relocate.SendToSelf();
            }
        }

        public void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                CreatureRelocationNotifier relocate = new(creature, GridType.All);

                cell.Visit(p, relocate, i_map, creature, i_radius);
            }
        }

        Map i_map;
        Cell cell;
        CellCoord p;
        float i_radius;
    }

    public class AIRelocationNotifier : IGridNotifierCreature
    {
        public GridType GridType { get; set; }
        public AIRelocationNotifier(Unit unit, GridType gridType)
        {
            i_unit = unit;
            isCreature = unit.IsTypeId(TypeId.Unit);
            GridType = gridType;
        }

        public void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                NotifierHelpers.CreatureUnitRelocationWorker(creature, i_unit);
                if (isCreature)
                    NotifierHelpers.CreatureUnitRelocationWorker(i_unit.ToCreature(), creature);
            }
        }

        Unit i_unit;
        bool isCreature;
    }

    public class PacketSenderRef : IDoWork<Player>
    {
        ServerPacket Data;

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

    public class MessageDistDeliverer<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
    {
        public GridType GridType { get; set; } = GridType.World;
        WorldObject i_source;
        T i_packetSender;
        PhaseShift i_phaseShift;
        float i_distSq;
        Team team;
        Player skipped_receiver;
        bool required3dDist;

        public MessageDistDeliverer(WorldObject src, T packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
        {
            i_source = src;
            i_packetSender = packetSender;
            i_phaseShift = src.GetPhaseShift();
            i_distSq = dist * dist;
            if (own_team_only && src.IsPlayer())
                team = src.ToPlayer().GetEffectiveTeam();

            skipped_receiver = skipped;
            required3dDist = req3dDist;
        }

        public void Visit(IList<Player> objs)
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
                {
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        if (visionPlayer.seerView == player)
                            SendPacket(visionPlayer);
                }

                if (player.seerView == player || player.GetVehicle() != null)
                    SendPacket(player);
            }
        }

        public void Visit(IList<Creature> objs)
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
                {
                    foreach (var visionPlayer in creature.GetSharedVisionList())
                        if (visionPlayer.seerView == creature)
                            SendPacket(visionPlayer);
                }
            }
        }

        public void Visit(IList<DynamicObject> objs)
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

        void SendPacket(Player player)
        {
            // never send packet to self
            if (i_source == player || (team != 0 && player.GetEffectiveTeam() != team) || skipped_receiver == player)
                return;

            if (!player.HaveAtClient(i_source))
                return;

            i_packetSender.Invoke(player);
        }
    }

    public class MessageDistDelivererToHostile<T> : IGridNotifierPlayer, IGridNotifierDynamicObject, IGridNotifierCreature where T : IDoWork<Player>
    {
        public GridType GridType { get; set; }
        Unit i_source;
        T i_packetSender;
        PhaseShift i_phaseShift;
        float i_distSq;

        public MessageDistDelivererToHostile(Unit src, T packetSender, float dist, GridType gridType)
        {
            i_source = src;
            i_packetSender = packetSender;
            i_phaseShift = src.GetPhaseShift();
            i_distSq = dist * dist;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
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
                {
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        if (visionPlayer.seerView == player)
                            SendPacket(visionPlayer);
                }

                if (player.seerView == player || player.GetVehicle())
                    SendPacket(player);
            }
        }

        public void Visit(IList<Creature> objs)
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
                {
                    foreach (var player in creature.GetSharedVisionList())
                        if (player.seerView == creature)
                            SendPacket(player);
                }
            }
        }

        public void Visit(IList<DynamicObject> objs)
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

        void SendPacket(Player player)
        {
            // never send packet to self
            if (player == i_source || !player.HaveAtClient(i_source) || player.IsFriendlyTo(i_source))
                return;

            i_packetSender.Invoke(player);
        }
    }

    public class UpdaterNotifier : IGridNotifierWorldObject
    {
        public GridType GridType { get; set; }
        public UpdaterNotifier(uint diff, GridType gridType)
        {
            i_timeDiff = diff;
            GridType = gridType;
        }

        public void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                WorldObject obj = objs[i];

                if (obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
                    continue;

                if (obj.IsInWorld)
                    obj.Update(i_timeDiff);
            }
        }

        uint i_timeDiff;
    }

    public class PlayerWorker : IGridNotifierPlayer
    {
        public GridType GridType { get; set; }
        PhaseShift i_phaseShift;
        Action<Player> action;

        public PlayerWorker(WorldObject searcher, Action<Player> _action, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            action = _action;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                if (player.InSamePhase(i_phaseShift))
                    action.Invoke(player);
            }
        }
    }

    public class CreatureWorker : IGridNotifierCreature
    {
        public GridType GridType { get; set; }
        PhaseShift i_phaseShift;
        IDoWork<Creature> Do;

        public CreatureWorker(WorldObject searcher, IDoWork<Creature> _Do, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            Do = _Do;
            GridType = gridType;
        }

        public void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                if (creature.InSamePhase(i_phaseShift))
                    Do.Invoke(creature);
            }
        }
    }

    public class GameObjectWorker : IGridNotifierGameObject
    {
        PhaseShift i_phaseShift;
        IDoWork<GameObject> _do;
        public GridType GridType { get; set; }

        public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> @do, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            _do = @do;
            GridType = gridType;
        }

        public void Visit(IList<GameObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];
                if (gameObject.InSamePhase(i_phaseShift))
                    _do.Invoke(gameObject);
            }
        }
    }

    public class WorldObjectWorker : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
    {
        public GridType GridType { get; set; }
        public GridMapTypeMask Mask { get; set; }
        PhaseShift i_phaseShift;
        IDoWork<WorldObject> i_do;

        public WorldObjectWorker(WorldObject searcher, IDoWork<WorldObject> _do, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
        {
            Mask = mapTypeMask;
            i_phaseShift = searcher.GetPhaseShift();
            i_do = _do;
            GridType = gridType;
        }

        public void Visit(IList<GameObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];
                if (gameObject.InSamePhase(i_phaseShift))
                    i_do.Invoke(gameObject);
            }
        }

        public void Visit(IList<Player> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                if (player.InSamePhase(i_phaseShift))
                    i_do.Invoke(player);
            }
        }

        public void Visit(IList<Creature> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                if (creature.InSamePhase(i_phaseShift))
                    i_do.Invoke(creature);
            }
        }

        public void Visit(IList<Corpse> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Corpse corpse = objs[i];
                if (corpse.InSamePhase(i_phaseShift))
                    i_do.Invoke(corpse);
            }
        }

        public void Visit(IList<DynamicObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];
                if (dynamicObject.InSamePhase(i_phaseShift))
                    i_do.Invoke(dynamicObject);
            }
        }

        public void Visit(IList<AreaTrigger> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                AreaTrigger areaTrigger = objs[i];
                if (areaTrigger.InSamePhase(i_phaseShift))
                    i_do.Invoke(areaTrigger);
            }
        }

        public void Visit(IList<SceneObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.SceneObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                SceneObject sceneObject = objs[i];
                if (sceneObject.InSamePhase(i_phaseShift))
                    i_do.Invoke(sceneObject);
            }
        }

        public void Visit(IList<Conversation> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Conversation conversation = objs[i];
                if (conversation.InSamePhase(i_phaseShift))
                    i_do.Invoke(conversation);
            }
        }
    }

    public class ResetNotifier : IGridNotifierPlayer, IGridNotifierCreature
    {
        public GridType GridType { get; set; }

        public ResetNotifier(GridType gridType)
        {
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                player.ResetAllNotifies();
            }
        }

        public void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                creature.ResetAllNotifies();
            }
        }
    }

    public class WorldObjectChangeAccumulator : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierDynamicObject
    {
        public GridType GridType { get; set; }
        public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d, GridType gridType)
        {
            updateData = d;
            worldObject = obj;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                BuildPacket(player);

                if (!player.GetSharedVisionList().Empty())
                {
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        BuildPacket(visionPlayer);
                }
            }
        }

        public void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                if (!creature.GetSharedVisionList().Empty())
                {
                    foreach (var visionPlayer in creature.GetSharedVisionList())
                        BuildPacket(visionPlayer);
                }
            }
        }

        public void Visit(IList<DynamicObject> objs)
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
                        if (caster.m_activePlayerData.FarsightObject == dynamicObject.GetGUID())
                            BuildPacket(caster);
                }
            }
        }

        void BuildPacket(Player player)
        {
            // Only send update once to a player
            if (!plr_list.Contains(player.GetGUID()) && player.HaveAtClient(worldObject))
            {
                worldObject.BuildFieldsUpdate(player, updateData);
                plr_list.Add(player.GetGUID());
            }
        }

        Dictionary<Player, UpdateData> updateData;
        WorldObject worldObject;
        List<ObjectGuid> plr_list = new();
    }

    public class PlayerDistWorker : IGridNotifierPlayer
    {
        public GridType GridType { get; set; }
        WorldObject i_searcher;
        float i_dist;
        IDoWork<Player> _do;

        public PlayerDistWorker(WorldObject searcher, float _dist, IDoWork<Player> @do, GridType gridType)
        {
            i_searcher = searcher;
            i_dist = _dist;
            _do = @do;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                if (player.InSamePhase(i_searcher) && player.IsWithinDist(i_searcher, i_dist))
                    _do.Invoke(player);
            }
        }
    }

    public class CallOfHelpCreatureInRangeDo : IDoWork<Creature>
    {
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

        Unit i_funit;
        Unit i_enemy;
        float i_range;
    }

    public class LocalizedDo : IDoWork<Player>
    {
        public LocalizedDo(MessageBuilder localizer)
        {
            _localizer = localizer;
        }

        public void Invoke(Player player)
        {
            Locale loc_idx = player.GetSession().GetSessionDbLocaleIndex();
            int cache_idx = (int)loc_idx + 1;
            IDoWork<Player> action;

            // create if not cached yet
            if (_localizedCache.Length < cache_idx + 1 || _localizedCache[cache_idx] == null)
            {
                if (_localizedCache.Length < cache_idx + 1)
                    Array.Resize(ref _localizedCache, cache_idx + 1);

                action = _localizer.Invoke(loc_idx);
                _localizedCache[cache_idx] = action;
            }
            else
                action = _localizedCache[cache_idx];

            action.Invoke(player);
        }

        MessageBuilder _localizer;
        IDoWork<Player>[] _localizedCache = new IDoWork<Player>[(int)Locale.Total];     // 0 = default, i => i-1 locale index
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
    public class WorldObjectSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
    {
        public GridMapTypeMask Mask { get; set; }
        public GridType GridType { get; set; }
        PhaseShift i_phaseShift;
        WorldObject i_object;
        ICheck<WorldObject> i_check;

        public WorldObjectSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
        {
            Mask = mapTypeMask;
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<GameObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
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

        public void Visit(IList<Player> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
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

        public void Visit(IList<Creature> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
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

        public void Visit(IList<Corpse> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
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

        public void Visit(IList<DynamicObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
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

        public void Visit(IList<AreaTrigger> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
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

        public void Visit(IList<SceneObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.SceneObject))
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

        public void Visit(IList<Conversation> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
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

        public WorldObject GetTarget() { return i_object; }
    }

    public class WorldObjectLastSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
    {
        public GridType GridType { get; set; }
        public GridMapTypeMask Mask { get; set; }
        PhaseShift i_phaseShift;
        WorldObject i_object;
        ICheck<WorldObject> i_check;

        public WorldObjectLastSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
        {
            Mask = mapTypeMask;
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<GameObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
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

        public void Visit(IList<Player> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
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

        public void Visit(IList<Creature> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
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

        public void Visit(IList<Corpse> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
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

        public void Visit(IList<DynamicObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
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

        public void Visit(IList<AreaTrigger> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
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

        public void Visit(IList<SceneObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.SceneObject))
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

        public void Visit(IList<Conversation> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
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

        public WorldObject GetTarget() { return i_object; }
    }

    public class WorldObjectListSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
    {
        public GridMapTypeMask Mask { get; set; }
        public GridType GridType { get; set; }
        List<WorldObject> i_objects;
        PhaseShift i_phaseShift;
        ICheck<WorldObject> i_check;

        public WorldObjectListSearcher(WorldObject searcher, List<WorldObject> objects, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
        {
            Mask = mapTypeMask;
            i_phaseShift = searcher.GetPhaseShift();
            i_objects = objects;
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                if (i_check.Invoke(player))
                    i_objects.Add(player);
            }
        }

        public void Visit(IList<Creature> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                if (i_check.Invoke(creature))
                    i_objects.Add(creature);
            }
        }

        public void Visit(IList<Corpse> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Corpse corpse = objs[i];
                if (i_check.Invoke(corpse))
                    i_objects.Add(corpse);
            }
        }

        public void Visit(IList<GameObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];
                if (i_check.Invoke(gameObject))
                    i_objects.Add(gameObject);
            }
        }

        public void Visit(IList<DynamicObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];
                if (i_check.Invoke(dynamicObject))
                    i_objects.Add(dynamicObject);
            }
        }

        public void Visit(IList<AreaTrigger> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                AreaTrigger areaTrigger = objs[i];
                if (i_check.Invoke(areaTrigger))
                    i_objects.Add(areaTrigger);
            }
        }

        public void Visit(IList<SceneObject> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                SceneObject sceneObject = objs[i];
                if (i_check.Invoke(sceneObject))
                    i_objects.Add(sceneObject);
            }
        }

        public void Visit(IList<Conversation> objs)
        {
            if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            for (var i = 0; i < objs.Count; ++i)
            {
                Conversation conversation = objs[i];
                if (i_check.Invoke(conversation))
                    i_objects.Add(conversation);
            }
        }
    }

    public class GameObjectSearcher : IGridNotifierGameObject
    {
        PhaseShift i_phaseShift;
        GameObject i_object;
        ICheck<GameObject> i_check;

        public GridType GridType { get; set; }

        public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<GameObject> objs)
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

        public GameObject GetTarget() { return i_object; }
    }

    public class GameObjectLastSearcher : IGridNotifierGameObject
    {
        PhaseShift i_phaseShift;
        GameObject i_object;
        ICheck<GameObject> i_check;
        public GridType GridType { get; set; }

        public GameObjectLastSearcher(WorldObject searcher, ICheck<GameObject> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<GameObject> objs)
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

        public GameObject GetTarget() { return i_object; }
    }

    public class GameObjectListSearcher : IGridNotifierGameObject
    {
        PhaseShift i_phaseShift;
        List<GameObject> i_objects;
        ICheck<GameObject> i_check;
        public GridType GridType { get; set; }

        public GameObjectListSearcher(WorldObject searcher, List<GameObject> objects, ICheck<GameObject> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_objects = objects;
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<GameObject> objs)
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

    public class UnitSearcher : IGridNotifierPlayer, IGridNotifierCreature
    {
        PhaseShift i_phaseShift;
        Unit i_object;
        ICheck<Unit> i_check;
        public GridType GridType { get; set; }

        public UnitSearcher(WorldObject searcher, ICheck<Unit> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
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

        public void Visit(IList<Creature> objs)
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

        public Unit GetTarget() { return i_object; }
    }

    public class UnitLastSearcher : IGridNotifierPlayer, IGridNotifierCreature
    {
        PhaseShift i_phaseShift;
        Unit i_object;
        ICheck<Unit> i_check;
        public GridType GridType { get; set; }

        public UnitLastSearcher(WorldObject searcher, ICheck<Unit> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
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

        public void Visit(IList<Creature> objs)
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

        public Unit GetTarget() { return i_object; }
    }

    public class UnitListSearcher : IGridNotifierCreature, IGridNotifierPlayer
    {
        PhaseShift i_phaseShift;
        List<Unit> i_objects;
        ICheck<Unit> i_check;
        public GridType GridType { get; set; }

        public UnitListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Unit> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_objects = objects;
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                if (player.InSamePhase(i_phaseShift))
                    if (i_check.Invoke(player))
                        i_objects.Add(player);
            }
        }

        public void Visit(IList<Creature> objs)
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

    public class CreatureSearcher : IGridNotifierCreature
    {
        PhaseShift i_phaseShift;
        Creature i_object;
        ICheck<Creature> i_check;
        public GridType GridType { get; set; }

        public CreatureSearcher(WorldObject searcher, ICheck<Creature> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Creature> objs)
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

        public Creature GetTarget() { return i_object; }
    }

    public class CreatureLastSearcher : IGridNotifierCreature
    {
        internal PhaseShift i_phaseShift;
        Creature i_object;
        ICheck<Creature> i_check;
        public GridType GridType { get; set; }

        public CreatureLastSearcher(WorldObject searcher, ICheck<Creature> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Creature> objs)
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

        public Creature GetTarget() { return i_object; }
    }

    public class CreatureListSearcher : IGridNotifierCreature
    {
        internal PhaseShift i_phaseShift;
        List<Creature> i_objects;
        ICheck<Creature> i_check;
        public GridType GridType { get; set; }


        public CreatureListSearcher(WorldObject searcher, List<Creature> objects, ICheck<Creature> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_objects = objects;
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Creature> objs)
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

    public class PlayerSearcher : IGridNotifierPlayer
    {
        public GridType GridType { get; set; }
        PhaseShift i_phaseShift;
        Player i_object;
        ICheck<Player> i_check;

        public PlayerSearcher(WorldObject searcher, ICheck<Player> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;    
        }

        public void Visit(IList<Player> objs)
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

        public Player GetTarget() { return i_object; }
    }

    public class PlayerLastSearcher : IGridNotifierPlayer
    {
        public GridType GridType { get; set; }
        PhaseShift i_phaseShift;
        Player i_object;
        ICheck<Player> i_check;

        public PlayerLastSearcher(WorldObject searcher, ICheck<Player> check, GridType gridType)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
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

        public Player GetTarget() { return i_object; }
    }

    public class PlayerListSearcher : IGridNotifierPlayer
    {
        public GridType GridType { get; set; }
        PhaseShift i_phaseShift;
        List<Unit> i_objects;
        ICheck<Player> i_check;

        public PlayerListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
        {
            i_phaseShift = searcher.GetPhaseShift();
            i_objects = objects;
            i_check = check;
            GridType = gridType;
        }

        public PlayerListSearcher(PhaseShift phaseShift, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
        {
            i_phaseShift = phaseShift;
            i_objects = objects;
            i_check = check;
            GridType = gridType;
        }

        public void Visit(IList<Player> objs)
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
        public MostHPMissingInRange(Unit obj, float range, uint hp)
        {
            i_obj = obj;
            i_range = range;
            i_hp = hp;
        }

        public bool Invoke(T u)
        {
            if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) && u.GetMaxHealth() - u.GetHealth() > i_hp)
            {
                i_hp = (uint)(u.GetMaxHealth() - u.GetHealth());
                return true;
            }
            return false;
        }

        Unit i_obj;
        float i_range;
        ulong i_hp;
    }

    class MostHPPercentMissingInRange : ICheck<Unit>
    {
        Unit _obj;
        float _range;
        float _minHpPct;
        float _maxHpPct;
        float _hpPct;

        public MostHPPercentMissingInRange(Unit obj, float range, uint minHpPct, uint maxHpPct)
        {
            _obj = obj;
            _range = range;
            _minHpPct = minHpPct;
            _maxHpPct = maxHpPct;
            _hpPct = 101.0f;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && u.IsInCombat() && !_obj.IsHostileTo(u) && _obj.IsWithinDist(u, _range) && _minHpPct <= u.GetHealthPct() && u.GetHealthPct() <= _maxHpPct && u.GetHealthPct() < _hpPct)
            {
                _hpPct = u.GetHealthPct();
                return true;
            }
            return false;
        }
    }

    public class FriendlyBelowHpPctEntryInRange : ICheck<Unit>
    {
        public FriendlyBelowHpPctEntryInRange(Unit obj, uint entry, float range, byte pct, bool excludeSelf)
        {
            i_obj = obj;
            i_entry = entry;
            i_range = range;
            i_pct = pct;
            i_excludeSelf = excludeSelf;
        }

        public bool Invoke(Unit u)
        {
            if (i_excludeSelf && i_obj.GetGUID() == u.GetGUID())
                return false;
            if (u.GetEntry() == i_entry && u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) && u.HealthBelowPct(i_pct))
                return true;
            return false;
        }

        Unit i_obj;
        uint i_entry;
        float i_range;
        byte i_pct;
        bool i_excludeSelf;
    }

    public class FriendlyCCedInRange : ICheck<Creature>
    {
        public FriendlyCCedInRange(Unit obj, float range)
        {
            i_obj = obj;
            i_range = range;
        }

        public bool Invoke(Creature u)
        {
            if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) &&
                (u.IsFeared() || u.IsCharmed() || u.HasRootAura() || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused)))
                return true;
            return false;
        }

        Unit i_obj;
        float i_range;
    }

    public class FriendlyMissingBuffInRange : ICheck<Creature>
    {
        public FriendlyMissingBuffInRange(Unit obj, float range, uint spellid)
        {
            i_obj = obj;
            i_range = range;
            i_spell = spellid;
        }

        public bool Invoke(Creature u)
        {
            if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) &&
                !(u.HasAura(i_spell)))
            {
                return true;
            }
            return false;
        }

        Unit i_obj;
        float i_range;
        uint i_spell;
    }

    public class AnyUnfriendlyUnitInObjectRangeCheck : ICheck<Unit>
    {
        public AnyUnfriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
        {
            i_obj = obj;
            i_funit = funit;
            i_range = range;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && i_obj.IsWithinDist(u, i_range) && !i_funit.IsFriendlyTo(u))
                return true;
            else
                return false;
        }

        WorldObject i_obj;
        Unit i_funit;
        float i_range;
    }

    public class NearestAttackableNoTotemUnitInObjectRangeCheck : ICheck<Unit>
    {
        public NearestAttackableNoTotemUnitInObjectRangeCheck(WorldObject obj, float range)
        {
            i_obj = obj;
            i_range = range;
        }

        public bool Invoke(Unit u)
        {
            if (!u.IsAlive())
                return false;

            if (u.GetCreatureType() == CreatureType.NonCombatPet)
                return false;

            if (u.IsTypeId(TypeId.Unit) && u.IsTotem())
                return false;

            if (!u.IsTargetableForAttack(false))
                return false;

            if (!i_obj.IsWithinDist(u, i_range) || i_obj.IsValidAttackTarget(u))
                return false;

            i_range = i_obj.GetDistance(u);
            return true;
        }

        WorldObject i_obj;
        float i_range;
    }

    public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
    {
        public AnyFriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            i_obj = obj;
            i_funit = funit;
            i_range = range;
            i_playerOnly = playerOnly;
            i_incOwnRadius = incOwnRadius;
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

            if (!u.IsInMap(i_obj) || !u.InSamePhase(i_obj) || !u.IsWithinDoubleVerticalCylinder(i_obj, searchRadius, searchRadius))
                return false;

            if (!i_funit.IsFriendlyTo(u))
                return false;

            return !i_playerOnly || u.GetTypeId() == TypeId.Player;
        }

        WorldObject i_obj;
        Unit i_funit;
        float i_range;
        bool i_playerOnly;
        bool i_incOwnRadius;
        bool i_incTargetRadius;
    }

    public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
    {
        public AnyGroupedUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool raid, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            _source = obj;
            _refUnit = funit;
            _range = range;
            _raid = raid;
            _playerOnly = playerOnly;
            i_incOwnRadius = incOwnRadius;
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
                return false;

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

        WorldObject _source;
        Unit _refUnit;
        float _range;
        bool _raid;
        bool _playerOnly;
        bool i_incOwnRadius;
        bool i_incTargetRadius;
    }

    public class AnyUnitInObjectRangeCheck : ICheck<Unit>
    {
        public AnyUnitInObjectRangeCheck(WorldObject obj, float range, bool check3D = true)
        {
            i_obj = obj;
            i_range = range;
            i_check3D = check3D;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && i_obj.IsWithinDist(u, i_range, i_check3D))
                return true;

            return false;
        }

        WorldObject i_obj;
        float i_range;
        bool i_check3D;
    }

    // Success at unit in range, range update for next check (this can be use with UnitLastSearcher to find nearest unit)
    public class NearestAttackableUnitInObjectRangeCheck : ICheck<Unit>
    {
        public NearestAttackableUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
        {
            i_obj = obj;
            i_funit = funit;
            i_range = range;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsTargetableForAttack() && i_obj.IsWithinDist(u, i_range) &&
                (i_funit.IsInCombatWith(u) || i_funit.IsHostileTo(u)) && i_obj.CanSeeOrDetect(u))
            {
                i_range = i_obj.GetDistance(u);        // use found unit range as new range limit for next check
                return true;
            }

            return false;
        }

        WorldObject i_obj;
        Unit i_funit;
        float i_range;
    }

    public class AnyAoETargetUnitInObjectRangeCheck : ICheck<Unit>
    {
        public AnyAoETargetUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, SpellInfo spellInfo = null, bool incOwnRadius = true, bool incTargetRadius = true)
        {
            i_obj = obj;
            i_funit = funit;
            _spellInfo = spellInfo;
            i_range = range;
            i_incOwnRadius = incOwnRadius;
            i_incTargetRadius = incTargetRadius;
        }

        public bool Invoke(Unit u)
        {
            // Check contains checks for: live, uninteractible, non-attackable flags, flight check and GM check, ignore totems
            if (u.IsTypeId(TypeId.Unit) && u.IsTotem())
                return false;

            if (_spellInfo != null)
            {
                if (!u.IsPlayer())
                {
                    if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                        return false;

                    if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && u.IsControlledByPlayer())
                        return false;
                }
                else if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                    return false;
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

        WorldObject i_obj;
        Unit i_funit;
        SpellInfo _spellInfo;
        float i_range;
        bool i_incOwnRadius;
        bool i_incTargetRadius;
    }

    public class AnyDeadUnitCheck : ICheck<Unit>
    {
        public bool Invoke(Unit u) { return !u.IsAlive(); }
    }

    public class NearestHostileUnitCheck : ICheck<Unit>
    {
        public NearestHostileUnitCheck(Creature creature, float dist = 0, bool playerOnly = false)
        {
            me = creature;
            i_playerOnly = playerOnly;

            m_range = (dist == 0 ? 9999 : dist);
        }

        public bool Invoke(Unit u)
        {
            if (!me.IsWithinDist(u, m_range))
                return false;

            if (!me.IsValidAttackTarget(u))
                return false;

            if (i_playerOnly && !u.IsTypeId(TypeId.Player))
                return false;

            m_range = me.GetDistance(u);   // use found unit range as new range limit for next check
            return true;
        }

        Creature me;
        float m_range;
        bool i_playerOnly;
    }

    class NearestHostileUnitInAttackDistanceCheck : ICheck<Unit>
    {
        public NearestHostileUnitInAttackDistanceCheck(Creature creature, float dist = 0)
        {
            me = creature;
            m_range = (dist == 0 ? 9999 : dist);
            m_force = (dist != 0);
        }

        public bool Invoke(Unit u)
        {
            if (!me.IsWithinDist(u, m_range))
                return false;

            if (!me.CanSeeOrDetect(u))
                return false;

            if (m_force)
            {
                if (!me.IsValidAttackTarget(u))
                    return false;
            }
            else if (!me.CanStartAttack(u, false))
                return false;

            m_range = me.GetDistance(u);   // use found unit range as new range limit for next check
            return true;
        }

        Creature me;
        float m_range;
        bool m_force;
    }

    class NearestHostileUnitInAggroRangeCheck : ICheck<Unit>
    {
        public NearestHostileUnitInAggroRangeCheck(Creature creature, bool useLOS = false, bool ignoreCivilians = false)
        {
            _me = creature;
            _useLOS = useLOS;
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

        Creature _me;
        bool _useLOS;
        bool _ignoreCivilians;
    }

    class AnyAssistCreatureInRangeCheck : ICheck<Creature>
    {
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

        Unit i_funit;
        Unit i_enemy;
        float i_range;
    }

    class NearestAssistCreatureInCreatureRangeCheck : ICheck<Creature>
    {
        public NearestAssistCreatureInCreatureRangeCheck(Creature obj, Unit enemy, float range)
        {
            i_obj = obj;
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

            i_range = i_obj.GetDistance(u);            // use found unit range as new range limit for next check
            return true;
        }

        Creature i_obj;
        Unit i_enemy;
        float i_range;
    }

    // Success at unit in range, range update for next check (this can be use with CreatureLastSearcher to find nearest creature)
    class NearestCreatureEntryWithLiveStateInObjectRangeCheck : ICheck<Creature>
    {
        public NearestCreatureEntryWithLiveStateInObjectRangeCheck(WorldObject obj, uint entry, bool alive, float range)
        {
            i_obj = obj;
            i_entry = entry;
            i_alive = alive;
            i_range = range;
        }

        public bool Invoke(Creature u)
        {
            if (u.GetDeathState() != DeathState.Dead && u.GetEntry() == i_entry && u.IsAlive() == i_alive && u.GetGUID() != i_obj.GetGUID() && i_obj.IsWithinDist(u, i_range) && u.CheckPrivateObjectOwnerVisibility(i_obj))
            {
                i_range = i_obj.GetDistance(u);         // use found unit range as new range limit for next check
                return true;
            }
            return false;
        }

        WorldObject i_obj;
        uint i_entry;
        bool i_alive;
        float i_range;
    }

    public class CreatureWithOptionsInObjectRangeCheck<T> : ICheck<Creature> where T : NoopCheckCustomizer
    {
        WorldObject i_obj;
        FindCreatureOptions i_args;
        T i_customizer;

        public CreatureWithOptionsInObjectRangeCheck(WorldObject obj, T customizer, FindCreatureOptions args)
        {
            i_obj = obj;
            i_args = args;
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

            if (i_args.CreatureId.HasValue && u.GetEntry() != i_args.CreatureId)
                return false;

            if (i_args.StringId != null && !u.HasStringId(i_args.StringId))
                return false;

            if (i_args.IsAlive.HasValue && u.IsAlive() != i_args.IsAlive)
                return false;

            if (i_args.IsSummon.HasValue && u.IsSummon() != i_args.IsSummon)
                return false;

            if (i_args.IsInCombat.HasValue && u.IsInCombat() != i_args.IsInCombat)
                return false;

            if ((i_args.OwnerGuid.HasValue && u.GetOwnerGUID() != i_args.OwnerGuid)
                || (i_args.CharmerGuid.HasValue && u.GetCharmerGUID() != i_args.CharmerGuid)
                || (i_args.CreatorGuid.HasValue && u.GetCreatorGUID() != i_args.CreatorGuid)
                || (i_args.DemonCreatorGuid.HasValue && u.GetDemonCreatorGUID() != i_args.DemonCreatorGuid)
                || (i_args.PrivateObjectOwnerGuid.HasValue && u.GetPrivateObjectOwner() != i_args.PrivateObjectOwnerGuid))
                return false;

            if (i_args.IgnorePrivateObjects && u.IsPrivateObject())
                return false;

            if (i_args.IgnoreNotOwnedPrivateObjects && !u.CheckPrivateObjectOwnerVisibility(i_obj))
                return false;

            if (i_args.AuraSpellId.HasValue && !u.HasAura((uint)i_args.AuraSpellId))
                return false;

            i_customizer.Update(u);
            return true;
        }
    }

    public class AnyPlayerInObjectRangeCheck : ICheck<Player>
    {
        public AnyPlayerInObjectRangeCheck(WorldObject obj, float range, bool reqAlive = true)
        {
            _obj = obj;
            _range = range;
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

        WorldObject _obj;
        float _range;
        bool _reqAlive;
    }

    class AnyPlayerInPositionRangeCheck : ICheck<Player>
    {
        public AnyPlayerInPositionRangeCheck(Position pos, float range, bool reqAlive = true)
        {
            _pos = pos;
            _range = range;
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

        Position _pos;
        float _range;
        bool _reqAlive;
    }

    class NearestPlayerInObjectRangeCheck : ICheck<Player>
    {
        public NearestPlayerInObjectRangeCheck(WorldObject obj, float range)
        {
            i_obj = obj;
            i_range = range;

        }

        public bool Invoke(Player pl)
        {
            if (pl.IsAlive() && i_obj.IsWithinDist(pl, i_range))
            {
                i_range = i_obj.GetDistance(pl);
                return true;
            }

            return false;
        }

        WorldObject i_obj;
        float i_range;
    }

    class AllFriendlyCreaturesInGrid : ICheck<Unit>
    {
        public AllFriendlyCreaturesInGrid(Unit obj)
        {
            unit = obj;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && u.IsVisible() && u.IsFriendlyTo(unit))
                return true;

            return false;
        }

        Unit unit;
    }

    class AllGameObjectsWithEntryInRange : ICheck<GameObject>
    {
        public AllGameObjectsWithEntryInRange(WorldObject obj, uint entry, float maxRange)
        {
            m_pObject = obj;
            m_uiEntry = entry;
            m_fRange = maxRange;
        }

        public bool Invoke(GameObject go)
        {
            if (m_uiEntry == 0 || go.GetEntry() == m_uiEntry && m_pObject.IsWithinDist(go, m_fRange, false))
                return true;

            return false;
        }

        WorldObject m_pObject;
        uint m_uiEntry;
        float m_fRange;
    }

    public class GetAllAlliesOfTargetCreaturesWithinRange : ICheck<Creature>
    {
        public GetAllAlliesOfTargetCreaturesWithinRange(Unit obj, float maxRange = 0f)
        {
            m_pObject = obj;
            m_fRange = maxRange;
        }

        public bool Invoke(Creature creature)
        {
            if (creature.IsHostileTo(m_pObject))
                return false;

            if (m_fRange != 0f)
            {
                if (m_fRange > 0.0f && !m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
                if (m_fRange < 0.0f && m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
            }

            return true;
        }

        Unit m_pObject;
        float m_fRange;
    }

    public class AllCreaturesWithinRange : ICheck<Creature>
    {
        public AllCreaturesWithinRange(WorldObject obj, float maxRange = 0f)
        {
            m_pObject = obj;
            m_fRange = maxRange;
        }

        public bool Invoke(Creature creature)
        {
            if (m_fRange != 0f)
            {
                if (m_fRange > 0.0f && !m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
                if (m_fRange < 0.0f && m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
            }

            return true;
        }

        WorldObject m_pObject;
        float m_fRange;
    }

    public class AllCreaturesOfEntryInRange : ICheck<Creature>
    {
        public AllCreaturesOfEntryInRange(WorldObject obj, uint entry, float maxRange = 0f)
        {
            m_pObject = obj;
            m_uiEntry = entry;
            m_fRange = maxRange;
        }

        public bool Invoke(Creature creature)
        {
            if (m_uiEntry != 0)
            {
                if (creature.GetEntry() != m_uiEntry)
                    return false;
            }

            if (m_fRange != 0f)
            {
                if (m_fRange > 0.0f && !m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
                if (m_fRange < 0.0f && m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
            }
            return true;
        }

        WorldObject m_pObject;
        uint m_uiEntry;
        float m_fRange;
    }

    public class AllCreaturesOfEntriesInRange : ICheck<Creature>
    {
        public AllCreaturesOfEntriesInRange(WorldObject obj, uint[] entry, float maxRange = 0f)
        {
            m_pObject = obj;
            m_uiEntry = entry;
            m_fRange = maxRange;
        }

        public bool Invoke(Creature creature)
        {
            if (m_uiEntry != null)
            {
                bool match = false;

                foreach (var entry in m_uiEntry)
                    if (entry != 0 && creature.GetEntry() == entry)
                        match = true;

                if (!match)
                    return false;
            }

            if (m_fRange != 0f)
            {
                if (m_fRange > 0.0f && !m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
                if (m_fRange < 0.0f && m_pObject.IsWithinDist(creature, m_fRange, false))
                    return false;
            }
            return true;
        }

        WorldObject m_pObject;
        uint[] m_uiEntry;
        float m_fRange;
    }

    class PlayerAtMinimumRangeAway : ICheck<Player>
    {
        public PlayerAtMinimumRangeAway(Unit _unit, float fMinRange)
        {
            unit = _unit;
            fRange = fMinRange;
        }

        public bool Invoke(Player player)
        {
            //No threat list check, must be done explicit if expected to be in combat with creature
            if (!player.IsGameMaster() && player.IsAlive() && !unit.IsWithinDist(player, fRange, false))
                return true;

            return false;
        }

        Unit unit;
        float fRange;
    }

    class GameObjectInRangeCheck : ICheck<GameObject>
    {
        public GameObjectInRangeCheck(float _x, float _y, float _z, float _range, uint _entry = 0)
        {
            x = _x;
            y = _y;
            z = _z;
            range = _range;
            entry = _entry;
        }

        public bool Invoke(GameObject go)
        {
            if (entry == 0 || (go.GetGoInfo() != null && go.GetGoInfo().entry == entry))
                return go.IsInRange(x, y, z, range);
            else return false;
        }

        float x, y, z, range;
        uint entry;
    }

    public class AllWorldObjectsInRange : ICheck<WorldObject>
    {
        public AllWorldObjectsInRange(WorldObject obj, float maxRange)
        {
            m_pObject = obj;
            m_fRange = maxRange;
        }

        public bool Invoke(WorldObject go)
        {
            return m_pObject.IsWithinDist(go, m_fRange, false) && m_pObject.InSamePhase(go);
        }

        WorldObject m_pObject;
        float m_fRange;
    }

    public class ObjectTypeIdCheck : ICheck<WorldObject>
    {
        public ObjectTypeIdCheck(TypeId typeId, bool equals)
        {
            _typeId = typeId;
            _equals = equals;
        }

        public bool Invoke(WorldObject obj)
        {
            return (obj.GetTypeId() == _typeId) == _equals;
        }

        TypeId _typeId;
        bool _equals;
    }

    public class ObjectGUIDCheck : ICheck<WorldObject>
    {
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

        ObjectGuid _GUID;
    }

    public class HeightDifferenceCheck : ICheck<WorldObject>
    {
        public HeightDifferenceCheck(WorldObject go, float diff, bool reverse)
        {
            _baseObject = go;
            _difference = diff;
            _reverse = reverse;

        }

        public bool Invoke(WorldObject unit)
        {
            return (unit.GetPositionZ() - _baseObject.GetPositionZ() > _difference) != _reverse;
        }

        WorldObject _baseObject;
        float _difference;
        bool _reverse;
    }

    public class UnitAuraCheck<T> : ICheck<T> where T : WorldObject
    {
        public UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default)
        {
            _present = present;
            _spellId = spellId;
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

        bool _present;
        uint _spellId;
        ObjectGuid _casterGUID;
    }

    class ObjectEntryAndPrivateOwnerIfExistsCheck : ICheck<WorldObject>
    {
        ObjectGuid _ownerGUID;
        uint _entry;

        public ObjectEntryAndPrivateOwnerIfExistsCheck(ObjectGuid ownerGUID, uint entry)
        {
            _ownerGUID = ownerGUID;
            _entry = entry;
        }

        public bool Invoke(WorldObject obj)
        {
            return obj.GetEntry() == _entry && (!obj.IsPrivateObject() || obj.GetPrivateObjectOwner() == _ownerGUID);
        }
    }

    class GameObjectFocusCheck : ICheck<GameObject>
    {
        public GameObjectFocusCheck(WorldObject caster, uint focusId)
        {
            _caster = caster;
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

        WorldObject _caster;
        uint _focusId;
    }

    // Find the nearest Fishing hole and return true only if source object is in range of hole
    class NearestGameObjectFishingHole : ICheck<GameObject>
    {
        public NearestGameObjectFishingHole(WorldObject obj, float range)
        {
            i_obj = obj;
            i_range = range;
        }

        public bool Invoke(GameObject go)
        {
            if (go.GetGoInfo().type == GameObjectTypes.FishingHole && go.IsSpawned() && i_obj.IsWithinDist(go, i_range) && i_obj.IsWithinDist(go, go.GetGoInfo().FishingHole.radius))
            {
                i_range = i_obj.GetDistance(go);
                return true;
            }
            return false;
        }

        WorldObject i_obj;
        float i_range;
    }

    class NearestGameObjectCheck : ICheck<GameObject>
    {
        public NearestGameObjectCheck(WorldObject obj)
        {
            i_obj = obj;
            i_range = 999;
        }

        public bool Invoke(GameObject go)
        {
            if (i_obj.IsWithinDist(go, i_range))
            {
                i_range = i_obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }

        WorldObject i_obj;
        float i_range;
    }

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO)
    class NearestGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
    {
        public NearestGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range, bool spawnedOnly = true)
        {
            _obj = obj;
            _entry = entry;
            _range = range;
            _spawnedOnly = spawnedOnly;
        }

        public bool Invoke(GameObject go)
        {
            if ((!_spawnedOnly || go.IsSpawned()) && go.GetEntry() == _entry && go.GetGUID() != _obj.GetGUID() && _obj.IsWithinDist(go, _range))
            {
                _range = _obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }

        WorldObject _obj;
        uint _entry;
        float _range;
        bool _spawnedOnly;
    }

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest unspawned GO)
    class NearestUnspawnedGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
    {
        WorldObject i_obj;
        uint i_entry;
        float i_range;

        public NearestUnspawnedGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range)
        {
            i_obj = obj;
            i_entry = entry;
            i_range = range;
        }

        public bool Invoke(GameObject go)
        {
            if (!go.IsSpawned() && go.GetEntry() == i_entry && go.GetGUID() != i_obj.GetGUID() && i_obj.IsWithinDist(go, i_range))
            {
                i_range = i_obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }
    }

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO with a certain type)
    class NearestGameObjectTypeInObjectRangeCheck : ICheck<GameObject>
    {
        public NearestGameObjectTypeInObjectRangeCheck(WorldObject obj, GameObjectTypes type, float range)
        {
            i_obj = obj;
            i_type = type;
            i_range = range;
        }

        public bool Invoke(GameObject go)
        {
            if (go.GetGoType() == i_type && i_obj.IsWithinDist(go, i_range))
            {
                i_range = i_obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }

        WorldObject i_obj;
        GameObjectTypes i_type;
        float i_range;
    }

    // CHECK modifiers
    public class NoopCheckCustomizer
    {
        public virtual bool Test(WorldObject o) { return true; }

        public virtual void Update(WorldObject o) { }
    }

    class NearestCheckCustomizer : NoopCheckCustomizer
    {
        WorldObject i_obj;
        float i_range;

        public NearestCheckCustomizer(WorldObject obj, float range)
        {
            i_obj = obj;
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
        public AnyDeadUnitObjectInRangeCheck(WorldObject searchObj, float range)
        {
            i_searchObj = searchObj;
            i_range = range;
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

        WorldObject i_searchObj;
        float i_range;
    }

    public class AnyDeadUnitSpellTargetInRangeCheck<T> : AnyDeadUnitObjectInRangeCheck<T> where T : WorldObject
    {
        public AnyDeadUnitSpellTargetInRangeCheck(WorldObject searchObj, float range, SpellInfo spellInfo, SpellTargetCheckTypes check, SpellTargetObjectTypes objectType) : base(searchObj, range)
        {
            i_check = new WorldObjectSpellTargetCheck(searchObj, searchObj, spellInfo, check, null, objectType);
        }

        public override bool Invoke(T obj)
        {
            return base.Invoke(obj) && i_check.Invoke(obj);
        }

        WorldObjectSpellTargetCheck i_check;
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