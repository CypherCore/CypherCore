/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Network;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace Game.Maps
{
    public class Notifier
    {
        public virtual void Visit(IList<WorldObject> objs) { }
        public virtual void Visit(IList<Creature> objs) { }
        public virtual void Visit(IList<AreaTrigger> objs) { }
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
                if (c.IsAIEnabled && c.CanSeeOrDetect(u, false, true))
                    c.GetAI().MoveInLineOfSight_Safe(u);
                else
                {
                    if (u.IsTypeId(TypeId.Player) && u.HasStealthAura() && c.IsAIEnabled && c.CanSeeOrDetect(u, false, true, true))
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
        public void Visit(IList<Conversation> conversations) { _notifier.Visit(conversations); }
        public void Visit(IList<GameObject> gameobjects) { _notifier.Visit(gameobjects); }
        public void Visit(IList<DynamicObject> dynamicobjects) { _notifier.Visit(dynamicobjects); }
        public void Visit(IList<Corpse> corpses) { _notifier.Visit(corpses); }
        public void Visit(IList<Player> players) { _notifier.Visit(players); }

        public void Visit(Dictionary<ObjectGuid, WorldObject> dict) { Visit(dict.Values.ToList()); }

        Notifier _notifier;
        internal GridMapTypeMask _mask;
    }

    public class VisibleNotifier : Notifier
    {
        public VisibleNotifier(Player pl)
        {
            i_player = pl;
            i_data = new UpdateData(pl.GetMapId());
            vis_guids = new List<ObjectGuid>(pl.m_clientGUIDs);
            i_visibleNow = new List<Unit>();
        }

        public override void Visit(IList<WorldObject> objs)
        {
            foreach (var obj in objs)
            {
                vis_guids.Remove(obj.GetGUID());
                i_player.UpdateVisibilityOf(obj, i_data, i_visibleNow);
            }
        }

        public void SendToSelf()
        {
            // at this moment i_clientGUIDs have guids that not iterate at grid level checks
            // but exist one case when this possible and object not out of range: transports
            Transport transport = i_player.GetTransport();
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
                                if (!obj.isNeedNotify(NotifyFlags.VisibilityChanged))
                                    obj.ToPlayer().UpdateVisibilityOf(i_player);
                                break;
                            case TypeId.Unit:
                                i_player.UpdateVisibilityOf(obj.ToCreature(), i_data, i_visibleNow);
                                break;
                            case TypeId.DynamicObject:
                                i_player.UpdateVisibilityOf(obj.ToDynamicObject(), i_data, i_visibleNow);
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
                    if (pl != null && pl.IsInWorld && !pl.isNeedNotify(NotifyFlags.VisibilityChanged))
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

    public class VisibleChangesNotifier : Notifier
    {
        public VisibleChangesNotifier(WorldObject obj)
        {
            i_object = obj;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                if (player.GetGUID() == i_object.GetGUID())
                    return;

                player.UpdateVisibilityOf(i_object);

                if (player.HasSharedVision())
                {
                    foreach (var visionPlayer in player.GetSharedVisionList())
                    {
                        if (visionPlayer.seerView == player)
                            visionPlayer.UpdateVisibilityOf(i_object);
                    }
                }
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
            {
                if (creature.HasSharedVision())
                {
                    foreach (var visionPlayer in creature.GetSharedVisionList())
                        if (visionPlayer.seerView == creature)
                            visionPlayer.UpdateVisibilityOf(i_object);
                }
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            foreach (var dynamicObj in objs)
            {
                Unit caster = dynamicObj.GetCaster();
                if (caster)
                {
                    Player pl = caster.ToPlayer();
                    if (pl && pl.seerView == dynamicObj)
                        pl.UpdateVisibilityOf(i_object);
                }
            }
        }

        WorldObject i_object;
    }

    public class PlayerRelocationNotifier : VisibleNotifier
    {
        public PlayerRelocationNotifier(Player player) : base(player) { }

        public override void Visit(IList<Player> objs)
        {
            base.Visit(objs);

            foreach (var player in objs)
            {
                vis_guids.Remove(player.GetGUID());

                i_player.UpdateVisibilityOf(player, i_data, i_visibleNow);

                if (player.seerView.isNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                player.UpdateVisibilityOf(i_player);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            base.Visit(objs);

            bool relocated_for_ai = (i_player == i_player.seerView);

            foreach (var creature in objs)
            {
                vis_guids.Remove(creature.GetGUID());

                i_player.UpdateVisibilityOf(creature, i_data, i_visibleNow);

                if (relocated_for_ai && !creature.isNeedNotify(NotifyFlags.VisibilityChanged))
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
            foreach (var player in objs)
            {
                if (!player.seerView.isNeedNotify(NotifyFlags.VisibilityChanged))
                    player.UpdateVisibilityOf(i_creature);

                CreatureUnitRelocationWorker(i_creature, player);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            if (!i_creature.IsAlive())
                return;

            foreach (var creature in objs)
            {
                CreatureUnitRelocationWorker(i_creature, creature);

                if (!creature.isNeedNotify(NotifyFlags.VisibilityChanged))
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
            foreach (var player in objs)
            {
                WorldObject viewPoint = player.seerView;

                if (!viewPoint.isNeedNotify(NotifyFlags.VisibilityChanged))
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
            foreach (var creature in objs)
            {
                if (!creature.isNeedNotify(NotifyFlags.VisibilityChanged))
                    continue;

                CreatureRelocationNotifier relocate = new CreatureRelocationNotifier(creature);

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
            foreach (var creature in objs)
            {
                CreatureUnitRelocationWorker(creature, i_unit);
                if (isCreature)
                    CreatureUnitRelocationWorker(i_unit.ToCreature(), creature);
            }
        }

        Unit i_unit;
        bool isCreature;
    }

    public class MessageDistDeliverer : Notifier
    {
        public MessageDistDeliverer(WorldObject src, ServerPacket msg, float dist, bool own_team_only = false, Player skipped = null)
        {
            i_source = src;
            i_message = msg;
            i_distSq = dist * dist;
            team = (uint)((own_team_only && src.IsTypeId(TypeId.Player)) ? ((Player)src).GetTeam() : 0);
            skipped_receiver = skipped;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                if (!player.IsInPhase(i_source))
                    continue;

                if (player.GetExactDist2dSq(i_source.GetPosition()) > i_distSq)
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
            foreach (var creature in objs)
            {
                if (creature.GetCreatureTemplate().Entry == 1)
                {

                }
                if (!creature.IsInPhase(i_source))
                    continue;

                if (creature.GetExactDist2dSq(i_source.GetPosition()) > i_distSq)
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
            foreach (var obj in objs)
            {
                if (!obj.IsInPhase(i_source))
                    continue;

                if (obj.GetExactDist2dSq(i_source.GetPosition()) > i_distSq)
                    continue;

                // Send packet back to the caster if the caster has vision of dynamic object
                Unit caster = obj.GetCaster();
                if (caster)
                {
                    Player player = caster.ToPlayer();
                    if (player && player.seerView == obj)
                        SendPacket(player);
                }
            }
        }

        void SendPacket(Player player)
        {
            // never send packet to self
            if (i_source == player || (team != 0 && (uint)player.GetTeam() != team) || skipped_receiver == player)
                return;

            if (!player.HaveAtClient(i_source))
                return;

            player.SendPacket(i_message);
        }

        WorldObject i_source;
        ServerPacket i_message;
        float i_distSq;
        uint team;
        Player skipped_receiver;
    }

    public class UpdaterNotifier : Notifier
    {
        public UpdaterNotifier(uint diff)
        {
            i_timeDiff = diff;
        }

        public override void Visit(IList<WorldObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                var obj = objs[i];
                if (obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
                    continue;

                if (obj.IsInWorld)
                    obj.Update(i_timeDiff);
            }
        }

        uint i_timeDiff;
    }

    public class PlayerWorker : Notifier
    {
        PlayerWorker(WorldObject searcher, Action<Player> _action)
        {
            _searcher = searcher;
            action = _action;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
                if (player.IsInPhase(_searcher))
                    action.Invoke(player);
        }

        WorldObject _searcher;
        Action<Player> action;
    }

    public class CreatureWorker : Notifier
    {
        public CreatureWorker(WorldObject searcher, IDoWork<Creature> _Do)
        {
            _searcher = searcher;
            Do = _Do;
        }

        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
            {
                if (creature.IsInPhase(_searcher))
                    Do.Invoke(creature);
            }
        }

        WorldObject _searcher;
        IDoWork<Creature> Do;
    }

    public class GameObjectWorker : Notifier
    {
        public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> _Do)
        {
            Do = _Do;
            _searcher = searcher;
        }

        public override void Visit(IList<GameObject> objs)
        {
            foreach (var obj in objs)
                if (obj.IsInPhase(_searcher))
                    Do.Invoke(obj);
        }

        WorldObject _searcher;
        IDoWork<GameObject> Do;
    }

    public class WorldObjectWorker : Notifier
    {
        public WorldObjectWorker(WorldObject searcher, IDoWork<WorldObject> _do, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            i_mapTypeMask = mapTypeMask;
            _searcher = searcher;
            i_do = _do;
        }

        public override void Visit(IList<WorldObject> objs)
        {
            foreach (var obj in objs)
            {
                switch (obj.GetTypeId())
                {
                    case TypeId.GameObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                            return;
                        break;
                    case TypeId.Player:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                            return;
                        break;
                    case TypeId.Unit:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                            return;
                        break;
                    case TypeId.Corpse:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                            return;
                        break;
                    case TypeId.DynamicObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                            return;
                        break;
                    case TypeId.AreaTrigger:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                            return;
                        break;
                    case TypeId.Conversation:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                            return;
                        break;
                }

                if (obj.IsInPhase(_searcher))
                    i_do.Invoke(obj);
            }
        }

        GridMapTypeMask i_mapTypeMask;
        WorldObject _searcher;
        IDoWork<WorldObject> i_do;
    }

    public class ResetNotifier : Notifier
    {
        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
                player.ResetAllNotifies();
        }
        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
                creature.ResetAllNotifies();
        }
    }

    public class WorldObjectChangeAccumulator : Notifier
    {
        public WorldObjectChangeAccumulator(WorldObject obj, Dictionary<Player, UpdateData> d)
        {
            updateData = d;
            worldObject = obj;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                BuildPacket(player);

                if (!player.GetSharedVisionList().Empty())
                {
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        BuildPacket(visionPlayer);
                }
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
            {
                if (!creature.GetSharedVisionList().Empty())
                {
                    foreach (var visionPlayer in creature.GetSharedVisionList())
                        BuildPacket(visionPlayer);
                }
            }
        }
        public override void Visit(IList<DynamicObject> objs)
        {
            foreach (var obj in objs)
            {
                ObjectGuid guid = obj.GetCasterGUID();

                if (guid.IsPlayer())
                {
                    //Caster may be NULL if DynObj is in removelist
                    Player caster = Global.ObjAccessor.FindPlayer(guid);
                    if (caster != null)
                        if (caster.GetGuidValue(ActivePlayerFields.Farsight) == obj.GetGUID())
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
        List<ObjectGuid> plr_list = new List<ObjectGuid>();
    }

    public class PlayerDistWorker : Notifier
    {
        public PlayerDistWorker(WorldObject searcher, float _dist, IDoWork<Player> _Do)
        {
            i_searcher = searcher;
            i_dist = _dist;
            Do = _Do;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                if (player.IsInPhase(i_searcher) && player.IsWithinDist(i_searcher, i_dist))
                    Do.Invoke(player);
            }
        }

        WorldObject i_searcher;
        float i_dist;
        IDoWork<Player> Do;
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
            if (!u.IsWithinDistInMap(i_funit, i_range))
                return;

            // only if see assisted creature's enemy
            if (!u.IsWithinLOSInMap(i_enemy))
                return;

            if (u.GetAI() != null)
                u.GetAI().AttackStart(i_enemy);
        }

        Unit i_funit;
        Unit i_enemy;
        float i_range;
    }

    public class LocalizedPacketDo : IDoWork<Player>
    {
        public LocalizedPacketDo(MessageBuilder builder)
        {
            Builder = builder;
        }

        public void Invoke(Player player)
        {
            LocaleConstant loc_idx = player.GetSession().GetSessionDbLocaleIndex();
            int cache_idx = (int)loc_idx + 1;
            ServerPacket data;

            // create if not cached yet
            if (i_data_cache.Length < cache_idx + 1 || i_data_cache[cache_idx] == null)
            {
                if (i_data_cache.Length < cache_idx + 1)
                    Array.Resize(ref i_data_cache, cache_idx + 1);

                data = Builder.Invoke(loc_idx);

                i_data_cache[cache_idx] = data;
            }
            else
                data = i_data_cache[cache_idx];

            player.SendPacket(data);
        }

        MessageBuilder Builder;
        ServerPacket[] i_data_cache = new ServerPacket[(int)LocaleConstant.Total];     // 0 = default, i => i-1 locale index
    }

    public class LocalizedPacketListDo : IDoWork<Player>
    {
        public LocalizedPacketListDo(MessageBuilder builder)
        {
            i_builder = builder;
        }

        public void Invoke(Player p)
        {
            LocaleConstant loc_idx = p.GetSession().GetSessionDbLocaleIndex();
            int cache_idx = (int)loc_idx + 1;
            List<ServerPacket> data_list = new List<ServerPacket>();

            // create if not cached yet
            if (i_data_cache.Count < cache_idx + 1 || i_data_cache[cache_idx].Empty())
            {
                data_list = i_data_cache[cache_idx];

                i_builder.Invoke(data_list, loc_idx);
            }
            else
                data_list = i_data_cache[cache_idx];

            foreach (var packet in data_list)
                p.SendPacket(packet);
        }

        MessageBuilder i_builder;
        MultiMap<int, ServerPacket> i_data_cache = new MultiMap<int, ServerPacket>();
        // 0 = default, i => i-1 locale index
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
        public WorldObjectSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            i_mapTypeMask = mapTypeMask;
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<WorldObject> objs)
        {
            // already found
            if (i_object)
                return;

            foreach (var obj in objs)
            {
                switch (obj.GetTypeId())
                {
                    case TypeId.Player:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                            continue;
                        break;
                    case TypeId.GameObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                            continue;
                        break;
                    case TypeId.Corpse:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                            continue;
                        break;
                    case TypeId.Unit:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                            continue;
                        break;
                    case TypeId.DynamicObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                            continue;
                        break;
                    case TypeId.AreaTrigger:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                            continue;
                        break;
                    case TypeId.Conversation:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                            continue;
                        break;
                }

                if (!obj.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(obj))
                {
                    i_object = obj;
                    return;
                }

            }
        }

        public WorldObject GetTarget() { return i_object; }

        GridMapTypeMask i_mapTypeMask;
        WorldObject i_object;
        WorldObject _searcher;
        ICheck<WorldObject> i_check;
    }
    public class WorldObjectLastSearcher : Notifier
    {
        public WorldObjectLastSearcher(WorldObject searcher, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            i_mapTypeMask = mapTypeMask;
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<WorldObject> objs)
        {
            foreach (var obj in objs)
            {
                switch (obj.GetTypeId())
                {
                    case TypeId.Player:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                            continue;
                        break;
                    case TypeId.GameObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                            continue;
                        break;
                    case TypeId.Corpse:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                            continue;
                        break;
                    case TypeId.Unit:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                            continue;
                        break;
                    case TypeId.DynamicObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                            continue;
                        break;
                    case TypeId.AreaTrigger:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                            continue;
                        break;
                    case TypeId.Conversation:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                            continue;
                        break;
                }

                if (!obj.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(obj))
                    i_object = obj;
            }
        }

        public WorldObject GetTarget() { return i_object; }

        GridMapTypeMask i_mapTypeMask;
        WorldObject i_object;
        WorldObject _searcher;
        ICheck<WorldObject> i_check;
    }
    public class WorldObjectListSearcher : Notifier
    {
        public WorldObjectListSearcher(WorldObject searcher, List<WorldObject> objects, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All)
        {
            i_mapTypeMask = mapTypeMask;
            _searcher = searcher;
            i_objects = objects;
            i_check = check;
        }

        public override void Visit(IList<WorldObject> objs)
        {
            foreach (var obj in objs)
            {
                switch (obj.GetTypeId())
                {
                    case TypeId.Player:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Player))
                            continue;
                        break;
                    case TypeId.GameObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.GameObject))
                            continue;
                        break;
                    case TypeId.Corpse:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Corpse))
                            continue;
                        break;
                    case TypeId.Unit:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Creature))
                            continue;
                        break;
                    case TypeId.DynamicObject:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.DynamicObject))
                            continue;
                        break;
                    case TypeId.AreaTrigger:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
                            continue;
                        break;
                    case TypeId.Conversation:
                        if (!i_mapTypeMask.HasAnyFlag(GridMapTypeMask.Conversation))
                            continue;
                        break;
                }

                if (i_check.Invoke(obj))
                    i_objects.Add(obj);
            }
        }

        GridMapTypeMask i_mapTypeMask;
        List<WorldObject> i_objects;
        WorldObject _searcher;
        ICheck<WorldObject> i_check;
    }

    public class GameObjectSearcher : Notifier
    {
        public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<GameObject> objs)
        {
            // already found
            if (i_object)
                return;

            foreach (var go in objs)
            {
                if (!go.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(go))
                {
                    i_object = go;
                    return;
                }
            }
        }

        public GameObject GetTarget() { return i_object; }

        WorldObject _searcher;
        GameObject i_object;
        ICheck<GameObject> i_check;
    }
    public class GameObjectLastSearcher : Notifier
    {
        public GameObjectLastSearcher(WorldObject searcher, ICheck<GameObject> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<GameObject> objs)
        {
            foreach (var go in objs)
            {
                if (!go.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(go))
                    i_object = go;
            }
        }

        public GameObject GetTarget() { return i_object; }

        WorldObject _searcher;
        GameObject i_object;
        ICheck<GameObject> i_check;
    }
    public class GameObjectListSearcher : Notifier
    {
        public GameObjectListSearcher(WorldObject searcher, List<GameObject> objects, ICheck<GameObject> check)
        {
            _searcher = searcher;
            i_objects = objects;
            i_check = check;
        }

        public override void Visit(IList<GameObject> objs)
        {
            foreach (var obj in objs)
            {
                if (obj.IsInPhase(_searcher))
                    if (i_check.Invoke(obj))
                        i_objects.Add(obj);
            }
        }

        WorldObject _searcher;
        List<GameObject> i_objects;
        ICheck<GameObject> i_check;
    }

    public class UnitSearcher : Notifier
    {
        public UnitSearcher(WorldObject searcher, ICheck<Unit> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                if (!player.IsInPhase(_searcher))
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
            foreach (var creature in objs)
            {
                if (!creature.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(creature))
                {
                    i_object = creature;
                    return;
                }
            }
        }

        public Unit GetTarget() { return i_object; }

        WorldObject _searcher;
        Unit i_object;
        ICheck<Unit> i_check;
    }
    public class UnitLastSearcher : Notifier
    {
        public UnitLastSearcher(WorldObject searcher, ICheck<Unit> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                if (!player.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(player))
                    i_object = player;
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
            {
                if (!creature.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(creature))
                    i_object = creature;
            }
        }

        public Unit GetTarget() { return i_object; }

        WorldObject _searcher;
        Unit i_object;
        ICheck<Unit> i_check;
    }
    public class UnitListSearcher : Notifier
    {
        public UnitListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Unit> check)
        {
            _searcher = searcher;
            i_objects = objects;
            i_check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                if (player.IsInPhase(_searcher))
                    if (i_check.Invoke(player))
                        i_objects.Add(player);
            }
        }
        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
            {
                if (creature.IsInPhase(_searcher))
                    if (i_check.Invoke(creature))
                        i_objects.Add(creature);
            }
        }

        WorldObject _searcher;
        List<Unit> i_objects;
        ICheck<Unit> i_check;
    }

    public class CreatureSearcher : Notifier
    {
        public CreatureSearcher(WorldObject searcher, ICheck<Creature> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<Creature> objs)
        {
            // already found
            if (i_object)
                return;

            foreach (var creature in objs)
            {
                if (!creature.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(creature))
                {
                    i_object = creature;
                    return;
                }
            }
        }

        public Creature GetTarget() { return i_object; }

        WorldObject _searcher;
        Creature i_object;
        ICheck<Creature> i_check;
    }
    public class CreatureLastSearcher : Notifier
    {
        public CreatureLastSearcher(WorldObject searcher, ICheck<Creature> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
            {
                if (!creature.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(creature))
                    i_object = creature;
            }
        }

        public Creature GetTarget() { return i_object; }

        WorldObject _searcher;
        Creature i_object;
        ICheck<Creature> i_check;
    }
    public class CreatureListSearcher : Notifier
    {
        public CreatureListSearcher(WorldObject searcher, List<Creature> objects, ICheck<Creature> check)
        {
            _searcher = searcher;
            i_objects = objects;
            i_check = check;
        }

        public override void Visit(IList<Creature> objs)
        {
            foreach (var creature in objs)
                if (creature.IsInPhase(_searcher))
                    if (i_check.Invoke(creature))
                        i_objects.Add(creature);
        }

        WorldObject _searcher;
        List<Creature> i_objects;
        ICheck<Creature> i_check;
    }

    public class PlayerSearcher : Notifier
    {
        public PlayerSearcher(WorldObject searcher, ICheck<Player> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            // already found
            if (i_object)
                return;

            foreach (var player in objs)
            {
                if (!player.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(player))
                {
                    i_object = player;
                    return;
                }
            }
        }

        public Player GetTarget() { return i_object; }

        WorldObject _searcher;
        Player i_object;
        ICheck<Player> i_check;
    }
    public class PlayerLastSearcher : Notifier
    {
        public PlayerLastSearcher(WorldObject searcher, ICheck<Player> check)
        {
            _searcher = searcher;
            i_check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
            {
                if (!player.IsInPhase(_searcher))
                    continue;

                if (i_check.Invoke(player))
                    i_object = player;
            }
        }

        public Player GetTarget() { return i_object; }

        WorldObject _searcher;
        Player i_object;
        ICheck<Player> i_check;
    }
    public class PlayerListSearcher : Notifier
    {
        public PlayerListSearcher(WorldObject searcher, List<Player> objects, ICheck<Player> check)
        {
            _searcher = searcher;
            i_objects = objects;
            i_check = check;
        }

        public override void Visit(IList<Player> objs)
        {
            foreach (var player in objs)
                if (player.IsInPhase(_searcher))
                    if (i_check.Invoke(player))
                        i_objects.Add(player);
        }

        WorldObject _searcher;
        List<Player> i_objects;
        ICheck<Player> i_check;
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
            if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDistInMap(u, i_range) && u.GetMaxHealth() - u.GetHealth() > i_hp)
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
            if (u.GetEntry() == i_entry && u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDistInMap(u, i_range) && u.HealthBelowPct(i_pct))
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
            if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDistInMap(u, i_range) &&
                (u.isFeared() || u.IsCharmed() || u.isFrozen() || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused)))
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
            if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDistInMap(u, i_range) &&
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
            if (u.IsAlive() && i_obj.IsWithinDistInMap(u, i_range) && !i_funit.IsFriendlyTo(u))
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
        public NearestAttackableNoTotemUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
        {
            i_obj = obj;
            i_funit = funit;
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

            if (!u.isTargetableForAttack(false))
                return false;

            if (!i_obj.IsWithinDistInMap(u, i_range) || i_funit._IsValidAttackTarget(u, null, i_obj))
                return false;

            i_range = i_obj.GetDistance(u);
            return true;
        }

        WorldObject i_obj;
        Unit i_funit;
        float i_range;
    }

    public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
    {
        public AnyFriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool playerOnly = false)
        {
            i_obj = obj;
            i_funit = funit;
            i_range = range;
            i_playerOnly = playerOnly;
        }

        public bool Invoke(Unit u)
        {
            if (u.IsAlive() && i_obj.IsWithinDistInMap(u, i_range) && i_funit.IsFriendlyTo(u) && (!i_playerOnly || u.IsTypeId(TypeId.Player)))
                return true;
            else
                return false;
        }

        WorldObject i_obj;
        Unit i_funit;
        float i_range;
        bool i_playerOnly;
    }

    public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
    {
        public AnyGroupedUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool raid, bool playerOnly = false)
        {
            _source = obj;
            _refUnit = funit;
            _range = range;
            _raid = raid;
            _playerOnly = playerOnly;
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

            return !_refUnit.IsHostileTo(u) && u.IsAlive() && _source.IsWithinDistInMap(u, _range);
        }

        WorldObject _source;
        Unit _refUnit;
        float _range;
        bool _raid;
        bool _playerOnly;
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
            if (u.IsAlive() && i_obj.IsWithinDistInMap(u, i_range, i_check3D))
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
            if (u.isTargetableForAttack() && i_obj.IsWithinDistInMap(u, i_range) &&
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
        public AnyAoETargetUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, SpellInfo spellInfo = null)
        {
            i_obj = obj;
            i_funit = funit;
            _spellInfo = spellInfo;
            i_range = range;

            if (_spellInfo == null)
            {
                DynamicObject dynObj = i_obj.ToDynamicObject();
                if (dynObj)
                    _spellInfo = dynObj.GetSpellInfo();
            }
        }

        public bool Invoke(Unit u)
        {
            // Check contains checks for: live, non-selectable, non-attackable flags, flight check and GM check, ignore totems
            if (u.IsTypeId(TypeId.Unit) && u.IsTotem())
                return false;

            if (_spellInfo != null && _spellInfo.HasAttribute(SpellAttr3.OnlyTargetPlayers) && !u.IsPlayer())
                return false;

            return i_funit._IsValidAttackTarget(u, _spellInfo, i_obj.IsTypeId(TypeId.DynamicObject) ? i_obj : null) && i_obj.IsWithinDistInMap(u, i_range);
        }

        WorldObject i_obj;
        Unit i_funit;
        SpellInfo _spellInfo;
        float i_range;
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
            if (!me.IsWithinDistInMap(u, m_range))
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
            m_force = (dist == 0 ? false : true);
        }

        public bool Invoke(Unit u)
        {
            if (!me.IsWithinDistInMap(u, m_range))
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
        public NearestHostileUnitInAggroRangeCheck(Creature creature, bool useLOS = false)
        {
            _me = creature;
            _useLOS = useLOS;
        }

        public bool Invoke(Unit u)
        {
            if (!u.IsHostileTo(_me))
                return false;

            if (!u.IsWithinDistInMap(_me, _me.GetAggroRange(u)))
                return false;

            if (!_me.IsValidAttackTarget(u))
                return false;

            if (_useLOS && !u.IsWithinLOSInMap(_me))
                return false;

            return true;
        }

        Creature _me;
        bool _useLOS;
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
            if (!i_funit.IsWithinDistInMap(u, i_range))
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

            if (!i_obj.IsWithinDistInMap(u, i_range))
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
            if (u.getDeathState() != DeathState.Dead && u.GetEntry() == i_entry && u.IsAlive() == i_alive && i_obj.IsWithinDistInMap(u, i_range))
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

            if (!_obj.IsWithinDistInMap(pl, _range))
                return false;

            return true;
        }

        WorldObject _obj;
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
            if (pl.IsAlive() && i_obj.IsWithinDistInMap(pl, i_range))
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

    class AllCreaturesOfEntryInRange : ICheck<Creature>
    {
        public AllCreaturesOfEntryInRange(WorldObject obj, uint entry, float maxRange)
        {
            m_pObject = obj;
            m_uiEntry = entry;
            m_fRange = maxRange;
        }

        public bool Invoke(Creature creature)
        {
            if (m_uiEntry == 0 || creature.GetEntry() == m_uiEntry && m_pObject.IsWithinDist(creature, m_fRange, false))
                return true;

            return false;
        }

        WorldObject m_pObject;
        uint m_uiEntry;
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
            return m_pObject.IsWithinDist(go, m_fRange, false) && m_pObject.IsInPhase(go);
        }

        WorldObject m_pObject;
        float m_fRange;
    }

    class ObjectTypeIdCheck : ICheck<WorldObject>
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

    class HeightDifferenceCheck : ICheck<WorldObject>
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
        public UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default(ObjectGuid))
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

    class GameObjectFocusCheck : ICheck<GameObject>
    {
        public GameObjectFocusCheck(Unit unit, uint focusId)
        {
            i_unit = unit;
            i_focusId = focusId;
        }

        public bool Invoke(GameObject go)
        {
            if (go.GetGoInfo().type != GameObjectTypes.SpellFocus)
                return false;

            if (go.GetGoInfo().SpellFocus.spellFocusType != i_focusId)
                return false;

            if (!go.isSpawned())
                return false;

            float dist = go.GetGoInfo().SpellFocus.radius / 2.0f;

            return go.IsWithinDistInMap(i_unit, dist);
        }

        Unit i_unit;
        uint i_focusId;
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
            if (go.GetGoInfo().type == GameObjectTypes.FishingHole && go.isSpawned() && i_obj.IsWithinDistInMap(go, i_range) && i_obj.IsWithinDistInMap(go, go.GetGoInfo().FishingHole.radius))
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
            if (i_obj.IsWithinDistInMap(go, i_range))
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
        public NearestGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range)
        {
            i_obj = obj;
            i_entry = entry;
            i_range = range;
        }

        public bool Invoke(GameObject go)
        {
            if (go.GetEntry() == i_entry && i_obj.IsWithinDistInMap(go, i_range))
            {
                i_range = i_obj.GetDistance(go);        // use found GO range as new range limit for next check
                return true;
            }
            return false;
        }

        WorldObject i_obj;
        uint i_entry;
        float i_range;
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
            if (go.GetGoType() == i_type && i_obj.IsWithinDistInMap(go, i_range))
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

    public class AnyDeadUnitObjectInRangeCheck<T> : ICheck<T> where T : WorldObject
    {
        public AnyDeadUnitObjectInRangeCheck(Unit searchObj, float range)
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

        Unit i_searchObj;
        float i_range;
    }

    public class AnyDeadUnitSpellTargetInRangeCheck<T> : AnyDeadUnitObjectInRangeCheck<T> where T : WorldObject
    {
        public AnyDeadUnitSpellTargetInRangeCheck(Unit searchObj, float range, SpellInfo spellInfo, SpellTargetCheckTypes check)
            : base(searchObj, range)
        {
            i_spellInfo = spellInfo;
            i_check = new WorldObjectSpellTargetCheck(searchObj, searchObj, spellInfo, check, null);
        }

        public override bool Invoke(T obj)
        {
            return base.Invoke(obj) && i_check.Invoke(obj);
        }

        SpellInfo i_spellInfo;
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