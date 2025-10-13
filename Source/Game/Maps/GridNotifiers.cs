// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol.Notification.V1;
using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Maps
{
    public class Notifier
    {
        public virtual void Visit(IList<WorldObject> objs) { }
        public virtual void Visit(IList<Creature> objs) { }
        public virtual void Visit(IList<AreaTrigger> objs) { }
        public virtual void Visit(IList<SceneObject> objs) { }
        public virtual void Visit(IList<Conversation> objs) { }
        public virtual void Visit(IList<GameObject> objs) { }
        public virtual void Visit(IList<DynamicObject> objs) { }
        public virtual void Visit(IList<Corpse> objs) { }
        public virtual void Visit(IList<Player> objs) { }

        public void CreatureUnitRelocationWorker(Creature c, Unit u)
        {
            if (!u.IsAlive() || !c.IsAlive() || c == u || u.IsInFlight())
                return;

            if (!c.HasUnitState(UnitState.Sightless))
            {
                if (c.IsAIEnabled() && c.CanSeeOrDetect(u, new CanSeeOrDetectExtraArgs() { DistanceCheck = true }))
                    c.GetAI().MoveInLineOfSight_Safe(u);
                else
                {
                    if (u.IsTypeId(TypeId.Player) && u.HasStealthAura() && c.IsAIEnabled() && c.CanSeeOrDetect(u, new CanSeeOrDetectExtraArgs() { DistanceCheck = true, AlertCheck = true }))
                        c.GetAI().TriggerAlert(u);
                }
            }
        }
    }

    public class Visitor
    {
        public Visitor(Notifier notifier, GridMapTypeMask mask)
        {
            _notifier = notifier;
            _mask = mask;
        }

        public void Visit(IList<WorldObject> collection) { _notifier.Visit(collection); }
        public void Visit(IList<Creature> creatures) { _notifier.Visit(creatures); }
        public void Visit(IList<AreaTrigger> areatriggers) { _notifier.Visit(areatriggers); }
        public void Visit(IList<SceneObject> sceneObjects) { _notifier.Visit(sceneObjects); }
        public void Visit(IList<Conversation> conversations) { _notifier.Visit(conversations); }
        public void Visit(IList<GameObject> gameobjects) { _notifier.Visit(gameobjects); }
        public void Visit(IList<DynamicObject> dynamicobjects) { _notifier.Visit(dynamicobjects); }
        public void Visit(IList<Corpse> corpses) { _notifier.Visit(corpses); }
        public void Visit(IList<Player> players) { _notifier.Visit(players); }

        Notifier _notifier;
        internal GridMapTypeMask _mask;
    }

    public class VisibleNotifier : Notifier
    {
        public VisibleNotifier(Player pl)
        {
            i_player = pl;
            i_data = new UpdateData(pl.GetMapId());
            vis_guids = [.. pl.m_clientGUIDs];
            i_visibleNow = new List<WorldObject>();
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
            if (transport != null)
            {
                foreach (var passenger in transport.GetPassengers())
                {
                    if (vis_guids.Remove(passenger.GetGUID()))
                    {
                        switch (passenger.GetTypeId())
                        {
                            case TypeId.GameObject:
                                i_player.UpdateVisibilityOf(passenger.ToGameObject(), i_data, i_visibleNow);
                                break;
                            case TypeId.Player:
                                i_player.UpdateVisibilityOf(passenger.ToPlayer(), i_data, i_visibleNow);
                                if (!passenger.IsNeedNotify(NotifyFlags.VisibilityChanged))
                                    passenger.ToPlayer().UpdateVisibilityOf(i_player);
                                break;
                            case TypeId.Unit:
                                i_player.UpdateVisibilityOf(passenger.ToCreature(), i_data, i_visibleNow);
                                break;
                            case TypeId.DynamicObject:
                                i_player.UpdateVisibilityOf(passenger.ToDynamicObject(), i_data, i_visibleNow);
                                break;
                            case TypeId.AreaTrigger:
                                i_player.UpdateVisibilityOf(passenger.ToAreaTrigger(), i_data, i_visibleNow);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            foreach (var outOfRangeGuid in vis_guids)
            {
                i_player.m_clientGUIDs.Remove(outOfRangeGuid);
                i_data.AddOutOfRangeGUID(outOfRangeGuid);

                if (outOfRangeGuid.IsPlayer())
                {
                    Player player = Global.ObjAccessor.FindPlayer(outOfRangeGuid);
                    if (player != null && !player.IsNeedNotify(NotifyFlags.VisibilityChanged))
                        player.UpdateVisibilityOf(i_player);
                }
            }

            if (!i_data.HasData())
                return;

            UpdateObject packet;
            i_data.BuildPacket(out packet);
            i_player.SendPacket(packet);

            foreach (var visibleObject in i_visibleNow)
                i_player.SendInitialVisiblePackets(visibleObject);
        }

        internal Player i_player;
        internal UpdateData i_data;
        internal List<ObjectGuid> vis_guids;
        internal List<WorldObject> i_visibleNow;
    }

    public class VisibleChangesNotifier : Notifier
    {
        ICollection<WorldObject> i_objects;

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
                {
                    if (visionPlayer.seerView == player)
                        visionPlayer.UpdateVisibilityOf(i_objects);
                }
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
                Unit caster = dynamicObject.GetCaster();
                if (caster != null)
                {
                    Player pl = caster.ToPlayer();
                    if (pl != null && pl.seerView == dynamicObject)
                        pl.UpdateVisibilityOf(i_objects);
                }
            }
        }
    }

    public class PlayerRelocationNotifier : VisibleNotifier
    {
        public PlayerRelocationNotifier(Player player) : base(player) { }

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

        Creature i_creature;
    }

    public class DelayedUnitRelocation : Notifier
    {
        public DelayedUnitRelocation(Cell c, CellCoord pair, Map map, float radius)
        {
            i_map = map;
            cell = c;
            p = pair;
            i_radius = radius;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                WorldObject viewPoint = player.seerView;

                if (!viewPoint.IsNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                if (player != viewPoint && !viewPoint.IsPositionValid())
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
                var c2grid_relocation = new Visitor(relocate, GridMapTypeMask.AllGrid);

                cell.Visit(p, c2world_relocation, i_map, creature, i_radius);
                cell.Visit(p, c2grid_relocation, i_map, creature, i_radius);
            }
        }

        Map i_map;
        Cell cell;
        CellCoord p;
        float i_radius;
    }

    public class AIRelocationNotifier : Notifier
    {
        public AIRelocationNotifier(Unit unit)
        {
            i_unit = unit;
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

        Unit i_unit;
        bool isCreature;
    }

    public class CreatureAggroGracePeriodExpiredNotifier : Notifier
    {
        Creature i_creature;

        public CreatureAggroGracePeriodExpiredNotifier(Creature c)
        {
            i_creature = c;
        }

        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
            {
                CreatureUnitRelocationWorker(creature, i_creature);
                CreatureUnitRelocationWorker(i_creature, creature);
            }
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
                CreatureUnitRelocationWorker(i_creature, player);
        }
    }

    public class PacketSenderRef(ServerPacket message)
    {
        public virtual void Invoke(Player player)
        {
            player.SendPacket(message);
        }

        public static implicit operator IDoWork<Player>(PacketSenderRef obj) => obj.Invoke;
    }

    public class PacketSenderOwning<T> where T : ServerPacket, new()
    {
        public T Data = new();

        public void Invoke(Player player)
        {
            player.SendPacket(Data);
        }

        public static implicit operator IDoWork<Player>(PacketSenderOwning<T> obj) => obj.Invoke;
    }

    public class MessageDistDeliverer : Notifier
    {
        WorldObject i_source;
        Action<Player> i_packetSender;
        PhaseShift i_phaseShift;
        float i_distSq;
        Team team;
        Player skipped_receiver;
        bool required3dDist;

        public MessageDistDeliverer(WorldObject src, IDoWork<Player> packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
        {
            i_source = src;
            i_packetSender = packetSender.Invoke;
            i_phaseShift = src.GetPhaseShift();
            i_distSq = dist * dist;
            if (own_team_only && src.IsPlayer())
                team = src.ToPlayer().GetEffectiveTeam();

            skipped_receiver = skipped;
            required3dDist = req3dDist;
        }

        public MessageDistDeliverer(WorldObject src, Action<Player> packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
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
                {
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        if (visionPlayer.seerView == player)
                            SendPacket(visionPlayer);
                }

                if (player.seerView == player || player.GetVehicle() != null)
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
                {
                    foreach (var visionPlayer in creature.GetSharedVisionList())
                        if (visionPlayer.seerView == creature)
                            SendPacket(visionPlayer);
                }
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
                if (caster != null)
                {
                    Player player = caster.ToPlayer();
                    if (player != null && player.seerView == dynamicObject)
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

    public class MessageDistDelivererToHostile<T> : Notifier
    {
        Unit i_source;
        IDoWork<Player> i_packetSender;
        PhaseShift i_phaseShift;
        float i_distSq;

        public MessageDistDelivererToHostile(Unit src, IDoWork<Player> packetSender, float dist)
        {
            i_source = src;
            i_packetSender = packetSender;
            i_phaseShift = src.GetPhaseShift();
            i_distSq = dist * dist;
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
                {
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        if (visionPlayer.seerView == player)
                            SendPacket(visionPlayer);
                }

                if (player.seerView == player || player.GetVehicle() != null)
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
                {
                    foreach (var player in creature.GetSharedVisionList())
                        if (player.seerView == creature)
                            SendPacket(player);
                }
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
                    if (player != null && player.seerView == dynamicObject)
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

    public class ObjectUpdater(uint diff) : Notifier
    {
        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                WorldObject obj = objs[i];

                if (obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
                    continue;

                if (obj.IsInWorld)
                    obj.Update(diff);
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

    public class WorldObjectChangeAccumulator
    {
        Dictionary<Player, UpdateData> updateData;
        WorldObject worldObject;
        HashSet<ObjectGuid> plr_list = new();

        public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d)
        {
            updateData = d;
            worldObject = obj;
        }

        public void Invoke(Player player)
        {
            // Only send update once to a player
            if (player.HaveAtClient(worldObject) && plr_list.Add(player.GetGUID()))
                worldObject.BuildFieldsUpdate(player, updateData);
        }

        public static implicit operator IDoWork<Player>(WorldObjectChangeAccumulator obj) => obj.Invoke;
    }

    //Searchers
    public enum WorldObjectSearcherContinuation
    {
        Continue,
        Return
    }

    public interface IResultInserter<T>
    {
        WorldObjectSearcherContinuation ShouldContinue();
        void Insert(T obj);
        T GetResult();
        bool HasResult();
    }

    class SearcherFirstObjectResult<T>() : IResultInserter<T>
    {
        T result;

        public WorldObjectSearcherContinuation ShouldContinue()
        {
            return result != null ? WorldObjectSearcherContinuation.Return : WorldObjectSearcherContinuation.Continue;
        }

        public void Insert(T obj)
        {
            result = obj;
        }

        public bool HasResult()
        {
            return result != null;
        }

        public T GetResult()
        {
            return result;
        }
    }

    class SearcherLastObjectResult<T>() : IResultInserter<T>
    {
        T result;

        public WorldObjectSearcherContinuation ShouldContinue()
        {
            return WorldObjectSearcherContinuation.Continue;
        }

        public void Insert(T obj)
        {
            result = obj;
        }

        public bool HasResult()
        {
            return result != null;
        }

        public T GetResult()
        {
            return result;
        }
    }

    class SearcherContainerResult<T>(List<T> container_) : IResultInserter<T>
    {
        ICollection<T> container = container_;

        public WorldObjectSearcherContinuation ShouldContinue()
        {
            return WorldObjectSearcherContinuation.Continue;
        }

        public void Insert(T obj)
        {
            container.Add(obj);
        }

        public bool HasResult()
        {
            return !container.Empty();
        }

        public T GetResult()
        {
            return default;
        }
    }

    struct DynamicGridMapTypeMaskCheck(GridMapTypeMask mask)
    {
        static Dictionary<Type, GridMapTypeMask> GridMapTypeMaskForType = new()
        {
            { typeof(Corpse), GridMapTypeMask.Corpse },
            { typeof(Creature), GridMapTypeMask.Creature },
            { typeof(DynamicObject), GridMapTypeMask.DynamicObject },
            { typeof(GameObject), GridMapTypeMask.GameObject },
            { typeof(Player), GridMapTypeMask.Player },
            { typeof(AreaTrigger), GridMapTypeMask.AreaTrigger },
            { typeof(SceneObject), GridMapTypeMask.SceneObject },
            { typeof(Conversation), GridMapTypeMask.Conversation }
        };
        GridMapTypeMask MaskValue = mask;

        public bool Includes(GridMapTypeMask mapTypeMask)
        {
            return (MaskValue & mapTypeMask) != 0;
        }

        public static GridMapTypeMask GetTypeMaskByType<T>()
        {
            return GridMapTypeMaskForType.LookupByKey(typeof(T));
        }
    }

    // WorldObject searchers & workers

    public class WorldObjectSearcherBase<T> : Notifier where T : WorldObject
    {
        GridMapTypeMask i_mapTypeMask;
        public PhaseShift i_phaseShift;
        ICheck<T> i_check;
        IResultInserter<T> resultInserter;

        public WorldObjectSearcherBase(PhaseShift phaseShift, IResultInserter<T> result, ICheck<T> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            i_mapTypeMask = mapTypeMask;
            i_phaseShift = phaseShift;
            i_check = check;
            resultInserter = result;
        }

        public override void Visit(IList<GameObject> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                return;

            VisitImpl(objs);
        }

        public override void Visit(IList<Player> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                return;

            VisitImpl(objs);
        }

        public override void Visit(IList<Creature> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                return;

            VisitImpl(objs);
        }

        public override void Visit(IList<Corpse> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                return;

            VisitImpl(objs);
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                return;

            VisitImpl(objs);
        }

        public override void Visit(IList<AreaTrigger> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                return;

            VisitImpl(objs);
        }

        public override void Visit(IList<SceneObject> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.SceneObject))
                return;

            VisitImpl(objs);
        }

        public override void Visit(IList<Conversation> objs)
        {
            if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                return;

            VisitImpl(objs);
        }

        void VisitImpl<TT>(IList<TT> objs) where TT : WorldObject
        {
            if (resultInserter.ShouldContinue() == WorldObjectSearcherContinuation.Return)
                return;

            foreach (dynamic obj in objs)
            {
                if (!obj.InSamePhase(i_phaseShift))
                    continue;

                if (i_check.Invoke(obj))
                {
                    resultInserter.Insert(obj);

                    if (resultInserter.ShouldContinue() == WorldObjectSearcherContinuation.Return)
                        return;
                }
            }
        }

        public bool HasResult()
        {
            return resultInserter.HasResult();
        }

        public T GetResult()
        {
            return resultInserter.GetResult();
        }
    }

    public class WorldObjectWorkerBase<T>(PhaseShift phaseShift, IDoWork<T> work, GridMapTypeMask mapTypeMask = GridMapTypeMask.All) : Notifier where T : WorldObject
    {
        DynamicGridMapTypeMaskCheck i_mapTypeMask = new(mapTypeMask);
        PhaseShift i_phaseShift = phaseShift;
        IDoWork<T> i_work = work;

        public override void Visit(IList<WorldObject> objs)
        {
            if (i_mapTypeMask.Includes(DynamicGridMapTypeMaskCheck.GetTypeMaskByType<T>()))
                VisitImpl(objs);
        }

        void VisitImpl(IList<WorldObject> objs)
        {
            foreach (var obj in objs)
                if (obj.InSamePhase(i_phaseShift))
                    i_work.Invoke(obj as T);
        }
    }

    public class WorldObjectSearcher : WorldObjectSearcherBase<WorldObject>
    {
        public WorldObjectSearcher(PhaseShift phaseShift, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
            : base(phaseShift, new SearcherFirstObjectResult<WorldObject>(), check, mapTypeMask) { }

        public WorldObjectSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
             : this(searcher.GetPhaseShift(), check, mapTypeMask) { }
    }

    public class WorldObjectLastSearcher : WorldObjectSearcherBase<WorldObject>
    {
        public WorldObjectLastSearcher(PhaseShift phaseShift, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
            : base(phaseShift, new SearcherLastObjectResult<WorldObject>(), check, mapTypeMask) { }

        public WorldObjectLastSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
            : this(searcher.GetPhaseShift(), check, mapTypeMask) { }
    }

    public class WorldObjectListSearcher : WorldObjectSearcherBase<WorldObject>
    {
        public WorldObjectListSearcher(PhaseShift phaseShift, List<WorldObject> container, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
            : base(phaseShift, new SearcherContainerResult<WorldObject>(container), check, mapTypeMask) { }

        public WorldObjectListSearcher(WorldObject searcher, List<WorldObject> container, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
            : this(searcher.GetPhaseShift(), container, check, mapTypeMask) { }
    }

    public class WorldObjectWorker<T> : WorldObjectWorkerBase<T> where T : WorldObject
    {
        public WorldObjectWorker(PhaseShift phaseShift, IDoWork<T> work, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
            : base(phaseShift, work, mapTypeMask) { }

        public WorldObjectWorker(WorldObject searcher, IDoWork<T> work, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
            : this(searcher.GetPhaseShift(), work, mapTypeMask) { }
    }

    // Gameobject searchers

    public class GameObjectSearcherBase : WorldObjectSearcherBase<GameObject>
    {
        public GameObjectSearcherBase(PhaseShift phaseShift, IResultInserter<GameObject> result, ICheck<GameObject> check)
           : base(phaseShift, result, check, GridMapTypeMask.GameObject) { }
    }

    public class GameObjectSearcher : GameObjectSearcherBase
    {
        public GameObjectSearcher(PhaseShift phaseShift, ICheck<GameObject> check)
            : base(phaseShift, new SearcherFirstObjectResult<GameObject>(), check) { }

        public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check)
            : this(searcher.GetPhaseShift(), check) { }
    }

    public class GameObjectLastSearcher : GameObjectSearcherBase
    {
        public GameObjectLastSearcher(PhaseShift phaseShift, ICheck<GameObject> check)
            : base(phaseShift, new SearcherLastObjectResult<GameObject>(), check) { }

        public GameObjectLastSearcher(WorldObject searcher, ICheck<GameObject> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class GameObjectListSearcher : GameObjectSearcherBase
    {
        public GameObjectListSearcher(PhaseShift phaseShift, List<GameObject> container, ICheck<GameObject> check)
                : base(phaseShift, new SearcherContainerResult<GameObject>(container), check) { }

        public GameObjectListSearcher(WorldObject searcher, List<GameObject> container, ICheck<GameObject> check)
                : this(searcher.GetPhaseShift(), container, check) { }
    }

    public class GameObjectWorker : WorldObjectWorkerBase<GameObject>
    {
        public GameObjectWorker(PhaseShift phaseShift, IDoWork<GameObject> work)
            : base(phaseShift, work, GridMapTypeMask.GameObject) { }

        public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> work)
              : this(searcher.GetPhaseShift(), work) { }
    }

    // Unit searchers

    public class UnitSearcherBase : WorldObjectSearcherBase<Unit>
    {
        public UnitSearcherBase(PhaseShift phaseShift, IResultInserter<Unit> result, ICheck<Unit> check)
           : base(phaseShift, result, check, GridMapTypeMask.Creature | GridMapTypeMask.Player) { }
    }

    // First accepted by Check Unit if any
    public class UnitSearcher : UnitSearcherBase
    {
        public UnitSearcher(PhaseShift phaseShift, ICheck<Unit> check)
            : base(phaseShift, new SearcherFirstObjectResult<Unit>(), check) { }

        public UnitSearcher(WorldObject searcher, ICheck<Unit> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    // Last accepted by Check Unit if any (Check can change requirements at each call)
    public class UnitLastSearcher : UnitSearcherBase
    {
        public UnitLastSearcher(PhaseShift phaseShift, ICheck<Unit> check)
                : base(phaseShift, new SearcherLastObjectResult<Unit>(), check) { }

        public UnitLastSearcher(WorldObject searcher, ICheck<Unit> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    // All accepted by Check units if any
    public class UnitListSearcher : UnitSearcherBase
    {
        public UnitListSearcher(PhaseShift phaseShift, List<Unit> container, ICheck<Unit> check)
                : base(phaseShift, new SearcherContainerResult<Unit>(container), check) { }

        public UnitListSearcher(WorldObject searcher, List<Unit> container, ICheck<Unit> check)
            : this(searcher.GetPhaseShift(), container, check) { }
    }

    public class UnitWorker : WorldObjectWorkerBase<Unit>
    {
        public UnitWorker(PhaseShift phaseShift, IDoWork<Unit> work)
                : base(phaseShift, work, GridMapTypeMask.Creature | GridMapTypeMask.Player) { }

        public UnitWorker(WorldObject searcher, IDoWork<Unit> work)
                    : this(searcher.GetPhaseShift(), work) { }
    }

    // Creature searchers

    public class CreatureSearcherBase : WorldObjectSearcherBase<Creature>
    {
        public CreatureSearcherBase(PhaseShift phaseShift, IResultInserter<Creature> result, ICheck<Creature> check)
           : base(phaseShift, result, check, GridMapTypeMask.Creature) { }
    }

    public class CreatureSearcher : CreatureSearcherBase
    {
        public CreatureSearcher(PhaseShift phaseShift, ICheck<Creature> check)
                : base(phaseShift, new SearcherFirstObjectResult<Creature>(), check) { }

        public CreatureSearcher(WorldObject searcher, ICheck<Creature> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    // Last accepted by Check Creature if any (Check can change requirements at each call)
    public class CreatureLastSearcher : CreatureSearcherBase
    {
        public CreatureLastSearcher(PhaseShift phaseShift, ICheck<Creature> check)
                : base(phaseShift, new SearcherLastObjectResult<Creature>(), check) { }

        public CreatureLastSearcher(WorldObject searcher, ICheck<Creature> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class CreatureListSearcher : CreatureSearcherBase
    {
        public CreatureListSearcher(PhaseShift phaseShift, List<Creature> container, ICheck<Creature> check)
                : base(phaseShift, new SearcherContainerResult<Creature>(container), check) { }

        public CreatureListSearcher(WorldObject searcher, List<Creature> container, ICheck<Creature> check)
                    : this(searcher.GetPhaseShift(), container, check) { }
    }

    public class CreatureWorker : WorldObjectWorkerBase<Creature>
    {
        public CreatureWorker(PhaseShift phaseShift, IDoWork<Creature> work)
                : base(phaseShift, work) { }

        public CreatureWorker(WorldObject searcher, IDoWork<Creature> work)
                : this(searcher.GetPhaseShift(), work) { }
    }

    // Player searchers

    public class PlayerSearcherBase : WorldObjectSearcherBase<Player>
    {
        public PlayerSearcherBase(PhaseShift phaseShift, IResultInserter<Player> result, ICheck<Player> check)
           : base(phaseShift, result, check, GridMapTypeMask.Player) { }
    }

    public class PlayerSearcher : PlayerSearcherBase
    {
        public PlayerSearcher(PhaseShift phaseShift, ICheck<Player> check)
                : base(phaseShift, new SearcherFirstObjectResult<Player>(), check) { }

        public PlayerSearcher(WorldObject searcher, ICheck<Player> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class PlayerLastSearcher : PlayerSearcherBase
    {
        public PlayerLastSearcher(PhaseShift phaseShift, ICheck<Player> check)
                : base(phaseShift, new SearcherLastObjectResult<Player>(), check) { }

        public PlayerLastSearcher(WorldObject searcher, ICheck<Player> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class PlayerListSearcher : PlayerSearcherBase
    {
        public PlayerListSearcher(PhaseShift phaseShift, List<Player> container, ICheck<Player> check)
                : base(phaseShift, new SearcherContainerResult<Player>(container), check) { }


        public PlayerListSearcher(WorldObject searcher, List<Player> container, ICheck<Player> check)
            : this(searcher.GetPhaseShift(), container, check) { }
    }

    public class PlayerWorker : WorldObjectWorkerBase<Player>
    {
        public PlayerWorker(PhaseShift phaseShift, IDoWork<Player> work)
                : base(phaseShift, work, GridMapTypeMask.Player) { }

        public PlayerWorker(WorldObject searcher, IDoWork<Player> work)
                    : this(searcher.GetPhaseShift(), work) { }
    }

    public class PlayerDistWorker : Notifier
    {
        WorldObject i_searcher;
        float i_dist;
        IDoWork<Player> i_work;

        public PlayerDistWorker(WorldObject searcher, float _dist, IDoWork<Player> work)
        {
            i_searcher = searcher;
            i_dist = _dist;
            i_work = work.Invoke;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];
                if (player.InSamePhase(i_searcher) && player.IsWithinDist(i_searcher, i_dist))
                    i_work(player);
            }
        }
    }

    // AreaTrigger searchers

    public class AreaTriggerSearcherBase : WorldObjectSearcherBase<AreaTrigger>
    {
        public AreaTriggerSearcherBase(PhaseShift phaseShift, IResultInserter<AreaTrigger> result, ICheck<AreaTrigger> check)
           : base(phaseShift, result, check, GridMapTypeMask.AreaTrigger) { }
    }

    public class AreaTriggerSearcher : AreaTriggerSearcherBase
    {
        public AreaTriggerSearcher(PhaseShift phaseShift, ICheck<AreaTrigger> check)
                : base(phaseShift, new SearcherFirstObjectResult<AreaTrigger>(), check) { }

        public AreaTriggerSearcher(WorldObject searcher, ICheck<AreaTrigger> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class AreaTriggerLastSearcher : AreaTriggerSearcherBase
    {
        public AreaTriggerLastSearcher(PhaseShift phaseShift, ICheck<AreaTrigger> check)
                : base(phaseShift, new SearcherLastObjectResult<AreaTrigger>(), check) { }

        public AreaTriggerLastSearcher(WorldObject searcher, ICheck<AreaTrigger> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class AreaTriggerListSearcher : AreaTriggerSearcherBase
    {
        public AreaTriggerListSearcher(PhaseShift phaseShift, List<AreaTrigger> container, ICheck<AreaTrigger> check)
                : base(phaseShift, new SearcherContainerResult<AreaTrigger>(container), check) { }

        public AreaTriggerListSearcher(WorldObject searcher, List<AreaTrigger> container, ICheck<AreaTrigger> check)
                    : this(searcher.GetPhaseShift(), container, check) { }
    }

    public class AreaTriggerWorker : WorldObjectWorkerBase<AreaTrigger>
    {
        public AreaTriggerWorker(PhaseShift phaseShift, IDoWork<AreaTrigger> work)
                : base(phaseShift, work, GridMapTypeMask.AreaTrigger) { }

        public AreaTriggerWorker(WorldObject searcher, IDoWork<AreaTrigger> work)
                    : this(searcher.GetPhaseShift(), work) { }
    }

    // SceneObject searchers

    public class SceneObjectSearcherBase : WorldObjectSearcherBase<SceneObject>
    {
        public SceneObjectSearcherBase(PhaseShift phaseShift, IResultInserter<SceneObject> result, ICheck<SceneObject> check)
           : base(phaseShift, result, check, GridMapTypeMask.SceneObject) { }
    }

    public class SceneObjectSearcher : SceneObjectSearcherBase
    {
        public SceneObjectSearcher(PhaseShift phaseShift, ICheck<SceneObject> check)
                : base(phaseShift, new SearcherFirstObjectResult<SceneObject>(), check) { }

        public SceneObjectSearcher(WorldObject searcher, ICheck<SceneObject> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class SceneObjectLastSearcher : SceneObjectSearcherBase
    {
        public SceneObjectLastSearcher(PhaseShift phaseShift, ICheck<SceneObject> check)
                : base(phaseShift, new SearcherLastObjectResult<SceneObject>(), check) { }

        public SceneObjectLastSearcher(WorldObject searcher, ICheck<SceneObject> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class SceneObjectListSearcher : SceneObjectSearcherBase
    {
        public SceneObjectListSearcher(PhaseShift phaseShift, List<SceneObject> container, ICheck<SceneObject> check)
                : base(phaseShift, new SearcherContainerResult<SceneObject>(container), check) { }

        public SceneObjectListSearcher(WorldObject searcher, List<SceneObject> container, ICheck<SceneObject> check)
            : this(searcher.GetPhaseShift(), container, check) { }
    }

    public class SceneObjectWorker : WorldObjectWorkerBase<SceneObject>
    {
        public SceneObjectWorker(PhaseShift phaseShift, IDoWork<SceneObject> work)
                : base(phaseShift, work, GridMapTypeMask.SceneObject) { }

        public SceneObjectWorker(WorldObject searcher, IDoWork<SceneObject> work)
                    : this(searcher.GetPhaseShift(), work) { }
    }

    // Conversation searchers

    public class ConversationSearcherBase : WorldObjectSearcherBase<Conversation>
    {
        public ConversationSearcherBase(PhaseShift phaseShift, IResultInserter<Conversation> result, ICheck<Conversation> check)
           : base(phaseShift, result, check, GridMapTypeMask.Conversation) { }
    }

    public class ConversationSearcher : ConversationSearcherBase
    {
        public ConversationSearcher(PhaseShift phaseShift, ICheck<Conversation> check)
                : base(phaseShift, new SearcherFirstObjectResult<Conversation>(), check) { }

        public ConversationSearcher(WorldObject searcher, ICheck<Conversation> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class ConversationLastSearcher : ConversationSearcherBase
    {
        public ConversationLastSearcher(PhaseShift phaseShift, ICheck<Conversation> check)
                : base(phaseShift, new SearcherLastObjectResult<Conversation>(), check) { }

        public ConversationLastSearcher(WorldObject searcher, ICheck<Conversation> check)
                    : this(searcher.GetPhaseShift(), check) { }
    }

    public class ConversationListSearcher : ConversationSearcherBase
    {
        public ConversationListSearcher(PhaseShift phaseShift, List<Conversation> container, ICheck<Conversation> check)
                : base(phaseShift, new SearcherContainerResult<Conversation>(container), check) { }

        public ConversationListSearcher(WorldObject searcher, List<Conversation> container, ICheck<Conversation> check)
            : this(searcher.GetPhaseShift(), container, check) { }
    }

    public class ConversationWorker : WorldObjectWorkerBase<Conversation>
    {
        public ConversationWorker(PhaseShift phaseShift, IDoWork<Conversation> work)
                : base(phaseShift, work, GridMapTypeMask.Conversation) { }

        public ConversationWorker(WorldObject searcher, IDoWork<Conversation> work)
                    : this(searcher.GetPhaseShift(), work) { }
    }

    // CHECKS && DO classes

    // CHECK modifiers
    public class InRangeCheckCustomizer
    {
        WorldObject i_obj;
        float i_range;

        public InRangeCheckCustomizer(WorldObject obj, float range)
        {
            i_obj = obj;
            i_range = range;
        }

        public virtual bool Test(WorldObject obj)
        {
            return i_obj.IsWithinDist(obj, i_range);
        }

        public virtual void Update(WorldObject obj) { }
    }

    public class NearestCheckCustomizer : InRangeCheckCustomizer
    {
        WorldObject i_obj;
        float i_range;

        public NearestCheckCustomizer(WorldObject obj, float range) : base(obj, range)
        {
            i_obj = obj;
            i_range = range;
        }

        public override bool Test(WorldObject obj)
        {
            return i_obj.IsWithinDist(obj, i_range);
        }

        public override void Update(WorldObject obj)
        {
            i_range = i_obj.GetDistance(obj);
        }
    }

    // WorldObject check classes

    public class AnyDeadUnitObjectInRangeCheck<T>(WorldObject searchObj, float range) : ICheck<T> where T : WorldObject
    {
        public virtual bool Invoke(T obj)
        {
            Player player = obj.ToPlayer();
            if (player != null)
                return !player.IsAlive() && !player.HasAuraType(AuraType.Ghost) && searchObj.IsWithinDistInMap(player, range);

            Creature creature = obj.ToCreature();
            if (creature != null)
                return !creature.IsAlive() && searchObj.IsWithinDistInMap(creature, range);

            Corpse corpse = obj.ToCorpse();
            if (corpse != null)
                return corpse.GetCorpseType() != CorpseType.Bones && searchObj.IsWithinDistInMap(corpse, range);

            return false;
        }
    }

    public class AnyDeadUnitSpellTargetInRangeCheck<T> : AnyDeadUnitObjectInRangeCheck<T> where T : WorldObject
    {
        WorldObjectSpellTargetCheck i_check;

        public AnyDeadUnitSpellTargetInRangeCheck(WorldObject searchObj, float range, SpellInfo spellInfo, SpellTargetCheckTypes check, SpellTargetObjectTypes objectType) : base(searchObj, range)
        {
            i_check = new WorldObjectSpellTargetCheck(searchObj, searchObj, spellInfo, check, null, objectType);
        }

        public override bool Invoke(T obj)
        {
            return base.Invoke(obj) && i_check.Invoke(obj);
        }
    }

    // WorldObject do classes

    public class RespawnDo
    {
        public void Invoke(Creature obj)
        {
            obj.Respawn();
        }

        public void Invoke(GameObject obj)
        {
            obj.Respawn();
        }

        public static implicit operator IDoWork<Creature>(RespawnDo obj) => obj.Invoke;
        public static implicit operator IDoWork<GameObject>(RespawnDo obj) => obj.Invoke;
    }

    // GameObject checks

    class GameObjectFocusCheck(WorldObject caster, uint focusId) : ICheck<GameObject>
    {
        public bool Invoke(GameObject go)
        {
            if (go.GetGoInfo().GetSpellFocusType() != focusId)
                return false;

            if (!go.IsSpawned())
                return false;

            float dist = go.GetGoInfo().GetSpellFocusRadius();
            return go.IsWithinDist(caster, dist);
        }
    }

    // Find the nearest Fishing hole and return true only if source object is in range of hole
    class NearestGameObjectFishingHole(WorldObject obj, float range) : ICheck<GameObject>
    {
        public bool Invoke(GameObject go)
        {
            if (go.GetGoInfo().type == GameObjectTypes.FishingHole && go.IsSpawned() && obj.IsWithinDist(go, range) && obj.IsWithinDist(go, go.GetGoInfo().FishingHole.radius))
            {
                range = obj.GetDistance(go);
                return true;
            }
            return false;
        }
    }

    class NearestGameObjectCheck(WorldObject obj) : ICheck<GameObject>
    {
        float i_range = 999f;

        public bool Invoke(GameObject go)
        {
            if (obj.IsWithinDist(go, i_range))
            {
                i_range = obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }
    }

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO)
    class NearestGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range, bool spawnedOnly = true) : ICheck<GameObject>
    {
        public bool Invoke(GameObject go)
        {
            if ((!spawnedOnly || go.IsSpawned()) && go.GetEntry() == entry && go.GetGUID() != obj.GetGUID() && obj.IsWithinDist(go, range))
            {
                range = obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }
    }

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest unspawned GO)
    class NearestUnspawnedGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range) : ICheck<GameObject>
    {
        public bool Invoke(GameObject go)
        {
            if (!go.IsSpawned() && go.GetEntry() == entry && go.GetGUID() != obj.GetGUID() && obj.IsWithinDist(go, range))
            {
                range = obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }
    }

    // Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO with a certain type)
    class NearestGameObjectTypeInObjectRangeCheck(WorldObject obj, GameObjectTypes type, float range) : ICheck<GameObject>
    {
        public bool Invoke(GameObject go)
        {
            if (go.GetGoType() == type && obj.IsWithinDist(go, range))
            {
                range = obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }
    }

    // Unit checks

    public class MostHPMissingInRange<T>(Unit obj, float range, uint hp) : ICheck<T> where T : Unit
    {
        public bool Invoke(T u)
        {
            if (u.IsAlive() && u.IsInCombat() && !obj.IsHostileTo(u) && obj.IsWithinDist(u, range) && u.GetMaxHealth() - u.GetHealth() > hp)
            {
                hp = (uint)(u.GetMaxHealth() - u.GetHealth());
                return true;
            }
            return false;
        }
    }

    class MostHPPercentMissingInRange(Unit obj, float range, uint minHpPct, uint maxHpPct) : ICheck<Unit>
    {
        float _hpPct = 101.0f;

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && u.IsInCombat() && !obj.IsHostileTo(u) && obj.IsWithinDist(u, range) && minHpPct <= u.GetHealthPct() && u.GetHealthPct() <= maxHpPct && u.GetHealthPct() < _hpPct)
            {
                _hpPct = u.GetHealthPct();
                return true;
            }
            return false;
        }
    }

    public class FriendlyBelowHpPctEntryInRange(Unit obj, uint entry, float range, byte pct, bool excludeSelf) : ICheck<Unit>
    {
        public bool Invoke(Unit u)
        {
            if (excludeSelf && obj.GetGUID() == u.GetGUID())
                return false;

            if (u.GetEntry() == entry && u.IsAlive() && u.IsInCombat() && !obj.IsHostileTo(u) && obj.IsWithinDist(u, range) && u.HealthBelowPct(pct))
                return true;

            return false;
        }
    }

    public class FriendlyCCedInRange(Unit obj, float range) : ICheck<Creature>
    {
        public bool Invoke(Creature u)
        {
            if (u.IsAlive() && u.IsInCombat() && !obj.IsHostileTo(u) && obj.IsWithinDist(u, range) &&
                (u.IsFeared() || u.IsCharmed() || u.HasRootAura() || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused)))
                return true;
            return false;
        }
    }

    public class FriendlyMissingBuffInRange(Unit obj, float range, uint spellid) : ICheck<Creature>
    {
        public bool Invoke(Creature u)
        {
            if (u.IsAlive() && u.IsInCombat() && !obj.IsHostileTo(u) && obj.IsWithinDist(u, range) && !u.HasAura(spellid))
                return true;

            return false;
        }
    }

    public class AnyUnfriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range) : ICheck<Unit>
    {
        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && obj.IsWithinDist(u, range) && !funit.IsFriendlyTo(u))
                return true;

            return false;
        }
    }

    public class NearestAttackableNoTotemUnitInObjectRangeCheck(WorldObject obj, float range) : ICheck<Unit>
    {
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

            if (!obj.IsWithinDist(u, range) || obj.IsValidAttackTarget(u))
                return false;

            range = obj.GetDistance(u);
            return true;
        }
    }

    public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
    {
        WorldObject i_obj;
        Unit i_funit;
        float i_range;
        bool i_playerOnly;
        bool i_incOwnRadius;
        bool i_incTargetRadius;

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

            if (!u.IsInMap(i_obj) || !u.InSamePhase(i_obj) || !u.IsWithinVerticalCylinder(i_obj, searchRadius, searchRadius, true))
                return false;

            if (!i_funit.IsFriendlyTo(u))
                return false;

            return !i_playerOnly || u.IsPlayer();
        }
    }

    public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
    {
        WorldObject _source;
        Unit _refUnit;
        float _range;
        bool _raid;
        bool _playerOnly;
        bool i_incOwnRadius;
        bool i_incTargetRadius;

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

            return u.IsInMap(_source) && u.InSamePhase(_source) && u.IsWithinVerticalCylinder(_source, searchRadius, searchRadius, true);
        }
    }

    public class AnyUnitInObjectRangeCheck : ICheck<Unit>
    {
        WorldObject i_obj;
        float i_range;
        bool i_check3D;
        bool i_reqAlive;

        public AnyUnitInObjectRangeCheck(WorldObject obj, float range, bool check3D = true, bool reqAlive = true)
        {
            i_obj = obj;
            i_range = range;
            i_check3D = check3D;
            i_reqAlive = reqAlive;
        }

        public bool Invoke(Unit u)
        {
            if (i_reqAlive && !u.IsAlive())
                return false;

            if (!i_obj.IsWithinDist(u, i_range, i_check3D))
                return false;

            return false;
        }
    }

    // Success at unit in range, range update for next check (this can be use with UnitLastSearcher to find nearest unit)
    public class NearestAttackableUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range) : ICheck<Unit>
    {
        public bool Invoke(Unit u)
        {
            if (u.IsTargetableForAttack() && obj.IsWithinDist(u, range) &&
                (funit.IsInCombatWith(u) || funit.IsHostileTo(u)) && obj.CanSeeOrDetect(u))
            {
                range = obj.GetDistance(u);        // use found unit range as new range limit for next check
                return true;
            }

            return false;
        }
    }

    public class AnyAoETargetUnitInObjectRangeCheck : ICheck<Unit>
    {
        WorldObject i_obj;
        Unit i_funit;
        SpellInfo _spellInfo;
        float i_range;
        bool i_incOwnRadius;
        bool i_incTargetRadius;

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

            return u.IsInMap(i_obj) && u.InSamePhase(i_obj) && u.IsWithinVerticalCylinder(i_obj, searchRadius, searchRadius, true);
        }
    }

    public class CallOfHelpCreatureInRangeDo(Unit funit, Unit enemy, float range)
    {
        public void Invoke(Creature u)
        {
            if (u == funit)
                return;

            if (!u.CanAssistTo(funit, enemy, false))
                return;

            // too far
            // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
            if (!u.IsWithinDist(funit, range, true, false, false))
                return;

            // only if see assisted creature's enemy
            if (!u.IsWithinLOSInMap(enemy))
                return;

            u.EngageWithTarget(enemy);
        }

        public static implicit operator IDoWork<Creature>(CallOfHelpCreatureInRangeDo obj) => obj.Invoke;
    }

    public class AnyDeadUnitCheck : ICheck<Unit>
    {
        public bool Invoke(Unit u) { return !u.IsAlive(); }
    }

    // Creature checks

    public class NearestHostileUnitCheck : ICheck<Unit>
    {
        Creature me;
        float m_range;
        bool i_playerOnly;

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

            if (i_playerOnly && !u.IsPlayer())
                return false;

            m_range = me.GetDistance(u);   // use found unit range as new range limit for next check
            return true;
        }
    }

    class NearestHostileUnitInAttackDistanceCheck : ICheck<Unit>
    {
        Creature me;
        float m_range;
        bool m_force;

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
    }

    class NearestHostileUnitInAggroRangeCheck : ICheck<Unit>
    {
        Creature _me;
        bool _useLOS;
        bool _ignoreCivilians;

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
                Creature creature = u.ToCreature();
                if (creature != null && creature.IsCivilian())
                    return false;
            }

            return true;
        }
    }

    class AnyAssistCreatureInRangeCheck(Unit funit, Unit enemy, float range) : ICheck<Creature>
    {
        public bool Invoke(Creature u)
        {
            if (u == funit)
                return false;

            if (!u.CanAssistTo(funit, enemy))
                return false;

            // too far
            // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
            if (!funit.IsWithinDist(u, range, true, false, false))
                return false;

            // only if see assisted creature
            if (!funit.IsWithinLOSInMap(u))
                return false;

            return true;
        }
    }

    class NearestAssistCreatureInCreatureRangeCheck(Creature obj, Unit enemy, float range) : ICheck<Creature>
    {
        public bool Invoke(Creature u)
        {
            if (u == obj)
                return false;

            if (!u.CanAssistTo(obj, enemy))
                return false;

            // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
            if (!obj.IsWithinDist(u, range, true, false, false))
                return false;

            if (!obj.IsWithinLOSInMap(u))
                return false;

            range = obj.GetDistance(u);            // use found unit range as new range limit for next check
            return true;
        }
    }

    // Success at unit in range, range update for next check (this can be use with CreatureLastSearcher to find nearest creature)
    class NearestCreatureEntryWithLiveStateInObjectRangeCheck : ICheck<Creature>
    {
        WorldObject i_obj;
        uint i_entry;
        bool i_alive;
        float i_range;

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
    }

    public class CreatureWithOptionsInObjectRangeCheck<T> : ICheck<Creature> where T : InRangeCheckCustomizer
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

            if (i_args.IsAlive.HasValue)
            {
                switch (i_args.IsAlive.Value)
                {
                    case FindCreatureAliveState.Alive:
                    {
                        if (!u.IsAlive())
                            return false;
                        break;
                    }
                    case FindCreatureAliveState.Dead:
                    {
                        if (u.IsAlive())
                            return false;
                        break;
                    }
                    case FindCreatureAliveState.EffectivelyAlive:
                    {
                        if (!u.IsAlive() || u.HasUnitFlag2(UnitFlags2.FeignDeath))
                            return false;
                        break;
                    }
                    case FindCreatureAliveState.EffectivelyDead:
                    {
                        if (u.IsAlive() && !u.HasUnitFlag2(UnitFlags2.FeignDeath))
                            return false;
                        break;
                    }
                    default:
                        break;
                }
            }

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

    class GameObjectWithOptionsInObjectRangeCheck<T> : ICheck<GameObject> where T : InRangeCheckCustomizer
    {
        WorldObject _obj;
        FindGameObjectOptions _args;
        T _customizer;

        public GameObjectWithOptionsInObjectRangeCheck(WorldObject obj, T customizer, FindGameObjectOptions args)
        {
            _obj = obj;
            _args = args;
            _customizer = customizer;
        }

        public bool Invoke(GameObject go)
        {
            if (_args.IsSpawned.HasValue && _args.IsSpawned != go.IsSpawned()) // Despawned
                return false;

            if (go.GetGUID() == _obj.GetGUID())
                return false;

            if (!_customizer.Test(go))
                return false;

            if (_args.GameObjectId.HasValue && go.GetEntry() != _args.GameObjectId)
                return false;

            if (!_args.StringId.IsEmpty() && !go.HasStringId(_args.StringId))
                return false;

            if (_args.IsSummon.HasValue && (go.GetSpawnId() == 0) != _args.IsSummon)
                return false;

            if ((_args.OwnerGuid.HasValue && go.GetOwnerGUID() != _args.OwnerGuid)
                || (_args.PrivateObjectOwnerGuid.HasValue && go.GetPrivateObjectOwner() != _args.PrivateObjectOwnerGuid))
                return false;

            if (_args.IgnorePrivateObjects && go.IsPrivateObject())
                return false;

            if (_args.IgnoreNotOwnedPrivateObjects && !go.CheckPrivateObjectOwnerVisibility(_obj))
                return false;

            if (_args.GameObjectType.HasValue && go.GetGoType() != _args.GameObjectType)
                return false;

            _customizer.Update(go);
            return true;
        }
    }

    class AnyPlayerInPositionRangeCheck(Position pos, float range, bool reqAlive = true) : ICheck<Player>
    {
        public bool Invoke(Player u)
        {
            if (reqAlive && !u.IsAlive())
                return false;

            if (!u.IsWithinDist3d(pos, range))
                return false;

            return true;
        }
    }

    class NearestPlayerInObjectRangeCheck(WorldObject obj, float range) : ICheck<Player>
    {
        public bool Invoke(Player pl)
        {
            if (pl.IsAlive() && obj.IsWithinDist(pl, range))
            {
                range = obj.GetDistance(pl);
                return true;
            }

            return false;
        }
    }

    class AllFriendlyCreaturesInGrid(Unit obj) : ICheck<Unit>
    {
        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && u.IsVisible() && u.IsFriendlyTo(obj))
                return true;

            return false;
        }
    }

    class AllGameObjectsWithEntryInRange(WorldObject obj, uint entry, float maxRange) : ICheck<GameObject>
    {
        public bool Invoke(GameObject go)
        {
            if (entry == 0 || go.GetEntry() == entry && obj.IsWithinDist(go, maxRange, false))
                return true;

            return false;
        }
    }

    public class AllCreaturesOfEntryInRange(WorldObject obj, uint entry, float maxRange = 0f) : ICheck<Creature>
    {
        public bool Invoke(Creature creature)
        {
            if (entry != 0)
            {
                if (creature.GetEntry() != entry)
                    return false;
            }

            if (maxRange != 0f)
            {
                if (maxRange > 0.0f && !obj.IsWithinDist(creature, maxRange, false))
                    return false;
                if (maxRange < 0.0f && obj.IsWithinDist(creature, maxRange, false))
                    return false;
            }
            return true;
        }
    }

    class PlayerAtMinimumRangeAway(Unit unit, float fMinRange) : ICheck<Player>
    {
        public bool Invoke(Player player)
        {
            //No threat list check, must be done explicit if expected to be in combat with creature
            if (!player.IsGameMaster() && player.IsAlive() && !unit.IsWithinDist(player, fMinRange, false))
                return true;

            return false;
        }
    }

    class GameObjectInRangeCheck : ICheck<GameObject>
    {
        float x, y, z, range;
        uint entry;

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

            return false;
        }
    }

    public class AllWorldObjectsInRange(WorldObject obj, float maxRange) : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject go)
        {
            return obj.IsWithinDist(go, maxRange, false) && obj.InSamePhase(go);
        }
    }

    public class ObjectTypeIdCheck(TypeId typeId, bool equals) : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject obj)
        {
            return (obj.GetTypeId() == typeId) == equals;
        }
    }

    public class ObjectGUIDCheck(ObjectGuid Guid) : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject obj)
        {
            return obj.GetGUID() == Guid;
        }

        //public static implicit operator Predicate<WorldObject>(ObjectGUIDCheck check) => check.Invoke;
    }

    public class HeightDifferenceCheck(WorldObject go, float diff, bool reverse) : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject unit)
        {
            return (unit.GetPositionZ() - go.GetPositionZ() > diff) != reverse;
        }
    }

    public class UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default) : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject obj)
        {
            return obj.ToUnit() != null && obj.ToUnit().HasAura(spellId, casterGUID) == present;
        }

        //public static implicit operator Predicate<WorldObject>(UnitAuraCheck unit) => unit.Invoke;
    }

    class ObjectEntryAndPrivateOwnerIfExistsCheck(ObjectGuid ownerGUID, uint entry) : ICheck<WorldObject>
    {
        public bool Invoke(WorldObject obj)
        {
            return obj.GetEntry() == entry && (!obj.IsPrivateObject() || obj.GetPrivateObjectOwner() == ownerGUID);
        }
    }

    class NearestAreaTriggerEntryInObjectRangeCheck : ICheck<AreaTrigger>
    {
        WorldObject i_obj;
        uint i_entry;
        float i_range;
        bool i_spawnedOnly;

        public NearestAreaTriggerEntryInObjectRangeCheck(WorldObject obj, uint entry, float range, bool spawnedOnly = false)
        {
            i_obj = obj;
            i_entry = entry;
            i_range = range;
            i_spawnedOnly = spawnedOnly;
        }

        public bool Invoke(AreaTrigger at)
        {
            if ((!i_spawnedOnly || at.IsStaticSpawn()) && at.GetEntry() == i_entry && at.GetGUID() != i_obj.GetGUID() && i_obj.IsWithinDist(at, i_range))
            {
                i_range = i_obj.GetDistance(at);
                return true;
            }
            return false;
        }
    }

    public class LocalizedDo(MessageBuilder localizer)
    {
        IDoWork<Player>[] _localizedCache = new IDoWork<Player>[(int)Locale.Total];     // 0 = default, i => i-1 locale index

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

                action = localizer.Invoke(loc_idx);
                _localizedCache[cache_idx] = action;
            }
            else
                action = _localizedCache[cache_idx];

            action.Invoke(player);
        }

        public static implicit operator IDoWork<Player>(LocalizedDo obj) => obj.Invoke;
    }
}