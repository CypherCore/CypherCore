﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.DataStorage;
using Game.Entities;
using Game.Movement;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.EnableTaxiNode, Processing = PacketProcessing.ThreadSafe)]
        void HandleEnableTaxiNodeOpcode(EnableTaxiNode enableTaxiNode)
        {
            var unit = GetPlayer().GetNPCIfCanInteractWith(enableTaxiNode.Unit, NPCFlags.FlightMaster, NPCFlags2.None);
            if (unit)
                SendLearnNewTaxiNode(unit);
        }

        [WorldPacketHandler(ClientOpcodes.TaxiNodeStatusQuery, Processing = PacketProcessing.ThreadSafe)]
        void HandleTaxiNodeStatusQuery(TaxiNodeStatusQuery taxiNodeStatusQuery)
        {
            SendTaxiStatus(taxiNodeStatusQuery.UnitGUID);
        }

        public void SendTaxiStatus(ObjectGuid guid)
        {
            // cheating checks
            var player = GetPlayer();
            var unit = ObjectAccessor.GetCreature(player, guid);
            if (!unit || unit.IsHostileTo(player) || !unit.HasNpcFlag(NPCFlags.FlightMaster))
            {
                Log.outDebug(LogFilter.Network, "WorldSession.SendTaxiStatus - {0} not found.", guid.ToString());
                return;
            }

            // find taxi node
            uint nearest = Global.ObjectMgr.GetNearestTaxiNode(unit.GetPositionX(), unit.GetPositionY(), unit.GetPositionZ(), unit.GetMapId(), player.GetTeam());

            var data = new TaxiNodeStatusPkt();
            data.Unit = guid;

            if (nearest == 0)
                data.Status = TaxiNodeStatus.None;
            else if (unit.GetReactionTo(player) >= ReputationRank.Neutral)
                data.Status = player.m_taxi.IsTaximaskNodeKnown(nearest) ? TaxiNodeStatus.Learned : TaxiNodeStatus.Unlearned;
            else
                data.Status = TaxiNodeStatus.NotEligible;

            SendPacket(data);
        }

        [WorldPacketHandler(ClientOpcodes.TaxiQueryAvailableNodes, Processing = PacketProcessing.ThreadSafe)]
        void HandleTaxiQueryAvailableNodes(TaxiQueryAvailableNodes taxiQueryAvailableNodes)
        {
            // cheating checks
            var unit = GetPlayer().GetNPCIfCanInteractWith(taxiQueryAvailableNodes.Unit, NPCFlags.FlightMaster, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleTaxiQueryAvailableNodes - {0} not found or you can't interact with him.", taxiQueryAvailableNodes.Unit.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            // unknown taxi node case
            if (SendLearnNewTaxiNode(unit))
                return;

            // known taxi node case
            SendTaxiMenu(unit);
        }

        public void SendTaxiMenu(Creature unit)
        {
            // find current node
            uint curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.GetPositionX(), unit.GetPositionY(), unit.GetPositionZ(), unit.GetMapId(), GetPlayer().GetTeam());
            if (curloc == 0)
                return;

            var lastTaxiCheaterState = GetPlayer().IsTaxiCheater();
            if (unit.GetEntry() == 29480)
                GetPlayer().SetTaxiCheater(true); // Grimwing in Ebon Hold, special case. NOTE: Not perfect, Zul'Aman should not be included according to WoWhead, and I think taxicheat includes it.

            var data = new ShowTaxiNodes();
            data.WindowInfo.HasValue = true;
            data.WindowInfo.Value.UnitGUID = unit.GetGUID();
            data.WindowInfo.Value.CurrentNode = (int)curloc;

            GetPlayer().m_taxi.AppendTaximaskTo(data, lastTaxiCheaterState);

            var reachableNodes = new byte[PlayerConst.TaxiMaskSize];
            Global.TaxiPathGraph.GetReachableNodesMask(CliDB.TaxiNodesStorage.LookupByKey(curloc), reachableNodes);
            for (var i = 0; i < PlayerConst.TaxiMaskSize; ++i)
            {
                data.CanLandNodes[i] &= reachableNodes[i];
                data.CanUseNodes[i] &= reachableNodes[i];
            }


            SendPacket(data);

            GetPlayer().SetTaxiCheater(lastTaxiCheaterState);
        }

        public void SendDoFlight(uint mountDisplayId, uint path, uint pathNode = 0)
        {
            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            GetPlayer().GetMotionMaster().Clear(MovementSlot.Controlled);

            if (mountDisplayId != 0)
                GetPlayer().Mount(mountDisplayId);

            GetPlayer().GetMotionMaster().MoveTaxiFlight(path, pathNode);
        }

        public bool SendLearnNewTaxiNode(Creature unit)
        {
            // find current node
            uint curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.GetPositionX(), unit.GetPositionY(), unit.GetPositionZ(), unit.GetMapId(), GetPlayer().GetTeam());

            if (curloc == 0)
                return true;

            if (GetPlayer().m_taxi.SetTaximaskNode(curloc))
            {
                SendPacket(new NewTaxiPath());

                var data = new TaxiNodeStatusPkt();
                data.Unit = unit.GetGUID();
                data.Status = TaxiNodeStatus.Learned;
                SendPacket(data);

                return true;
            }
            else
                return false;
        }

        public void SendDiscoverNewTaxiNode(uint nodeid)
        {
            if (GetPlayer().m_taxi.SetTaximaskNode(nodeid))
                SendPacket(new NewTaxiPath());
        }

        [WorldPacketHandler(ClientOpcodes.ActivateTaxi, Processing = PacketProcessing.ThreadSafe)]
        void HandleActivateTaxi(ActivateTaxi activateTaxi)
        {
            var unit = GetPlayer().GetNPCIfCanInteractWith(activateTaxi.Vendor, NPCFlags.FlightMaster, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleActivateTaxiOpcode - {0} not found or you can't interact with it.", activateTaxi.Vendor.ToString());
                SendActivateTaxiReply(ActivateTaxiReply.TooFarAway);
                return;
            }

            uint curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.GetPositionX(), unit.GetPositionY(), unit.GetPositionZ(), unit.GetMapId(), GetPlayer().GetTeam());
            if (curloc == 0)
                return;

            var from = CliDB.TaxiNodesStorage.LookupByKey(curloc);
            var to = CliDB.TaxiNodesStorage.LookupByKey(activateTaxi.Node);
            if (to == null)
                return;

            if (!GetPlayer().IsTaxiCheater())
            {
                if (!GetPlayer().m_taxi.IsTaximaskNodeKnown(curloc) || !GetPlayer().m_taxi.IsTaximaskNodeKnown(activateTaxi.Node))
                {
                    SendActivateTaxiReply(ActivateTaxiReply.NotVisited);
                    return;
                }
            }

            uint preferredMountDisplay = 0;
            var mount = CliDB.MountStorage.LookupByKey(activateTaxi.FlyingMountID);
            if (mount != null)
            {
                if (GetPlayer().HasSpell(mount.SourceSpellID))
                {
                    var mountDisplays = Global.DB2Mgr.GetMountDisplays(mount.Id);
                    if (mountDisplays != null)
                    {
                        var usableDisplays = mountDisplays.Where(mountDisplay =>
                        {
                            var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mountDisplay.PlayerConditionID);
                            if (playerCondition != null)
                                return ConditionManager.IsPlayerMeetingCondition(GetPlayer(), playerCondition);

                            return true;
                        }).ToList();

                        if (!usableDisplays.Empty())
                            preferredMountDisplay = usableDisplays.SelectRandom().CreatureDisplayInfoID;
                    }
                }
            }

            var nodes = new List<uint>();
            Global.TaxiPathGraph.GetCompleteNodeRoute(from, to, GetPlayer(), nodes);
            GetPlayer().ActivateTaxiPathTo(nodes, unit, 0, preferredMountDisplay);
        }

        public void SendActivateTaxiReply(ActivateTaxiReply reply = ActivateTaxiReply.Ok)
        {
            var data = new ActivateTaxiReplyPkt();
            data.Reply = reply;
            SendPacket(data);
        }

        [WorldPacketHandler(ClientOpcodes.TaxiRequestEarlyLanding, Processing = PacketProcessing.ThreadSafe)]
        void HandleTaxiRequestEarlyLanding(TaxiRequestEarlyLanding taxiRequestEarlyLanding)
        {
            if (GetPlayer().GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Flight)
            {
                if (GetPlayer().m_taxi.RequestEarlyLanding())
                {
                    var flight = (FlightPathMovementGenerator)GetPlayer().GetMotionMaster().Top();
                    flight.LoadPath(GetPlayer(), flight.GetPath()[(int)flight.GetCurrentNode()].NodeIndex);
                    flight.Reset(GetPlayer());
                }
            }
        }
    }
}
