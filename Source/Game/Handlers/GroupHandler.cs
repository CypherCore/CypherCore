// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using System;

namespace Game
{
    public partial class WorldSession
    {
        public void SendPartyResult(PartyOperation operation, string member, PartyResult res, uint val = 0)
        {
            PartyCommandResult packet = new();

            packet.Name = member;
            packet.Command = (byte)operation;
            packet.Result = (byte)res;
            packet.ResultData = val;
            packet.ResultGUID = ObjectGuid.Empty;

            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.PartyInvite)]
        void HandlePartyInvite(PartyInviteClient packet)
        {
            Player invitingPlayer = GetPlayer();
            Player invitedPlayer = Global.ObjAccessor.FindPlayerByName(packet.TargetName);

            // no player
            if (invitedPlayer == null)
            {
                SendPartyResult(PartyOperation.Invite, packet.TargetName, PartyResult.BadPlayerNameS);
                return;
            }

            // player trying to invite himself (most likely cheating)
            if (invitedPlayer == invitingPlayer)
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.BadPlayerNameS);
                return;
            }

            // restrict invite to GMs
            if (!WorldConfig.GetBoolValue(WorldCfg.AllowGmGroup) && !invitingPlayer.IsGameMaster() && invitedPlayer.IsGameMaster())
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.BadPlayerNameS);
                return;
            }

            // can't group with
            if (!invitingPlayer.IsGameMaster() && !WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && invitingPlayer.GetTeam() != invitedPlayer.GetTeam())
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.PlayerWrongFaction);
                return;
            }
            if (invitingPlayer.GetInstanceId() != 0 && invitedPlayer.GetInstanceId() != 0 && invitingPlayer.GetInstanceId() != invitedPlayer.GetInstanceId() && invitingPlayer.GetMapId() == invitedPlayer.GetMapId())
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.TargetNotInInstanceS);
                return;
            }
            // just ignore us
            if (invitedPlayer.GetInstanceId() != 0 && invitedPlayer.GetDungeonDifficultyID() != invitingPlayer.GetDungeonDifficultyID())
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.IgnoringYouS);
                return;
            }

            if (invitedPlayer.GetSocial().HasIgnore(invitingPlayer.GetGUID(), invitingPlayer.GetSession().GetAccountGUID()))
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.IgnoringYouS);
                return;
            }

            if (!invitedPlayer.GetSocial().HasFriend(invitingPlayer.GetGUID()) && invitingPlayer.GetLevel() < WorldConfig.GetIntValue(WorldCfg.PartyLevelReq))
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.InviteRestricted);
                return;
            }

            Group group = invitingPlayer.GetGroup(packet.PartyIndex);
            if (group == null)
                group = invitingPlayer.GetGroupInvite();

            Group group2 = invitedPlayer.GetGroup(packet.PartyIndex);
            PartyInvite partyInvite;
            // player already in another group or invited
            if (group2 != null || invitedPlayer.GetGroupInvite() != null)
            {
                SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.AlreadyInGroupS);

                if (group2 != null)
                {
                    // tell the player that they were invited but it failed as they were already in a group
                    partyInvite = new PartyInvite();
                    partyInvite.Initialize(invitingPlayer, packet.ProposedRoles, false);
                    invitedPlayer.SendPacket(partyInvite);
                }

                return;
            }

            if (group != null)
            {
                // not have permissions for invite
                if (!group.IsLeader(invitingPlayer.GetGUID()) && !group.IsAssistant(invitingPlayer.GetGUID()))
                {
                    if (group.IsCreated())
                        SendPartyResult(PartyOperation.Invite, "", PartyResult.NotLeader);
                    return;
                }
                // not have place
                if (group.IsFull())
                {
                    SendPartyResult(PartyOperation.Invite, "", PartyResult.GroupFull);
                    return;
                }
            }

            // ok, but group not exist, start a new group
            // but don't create and save the group to the DB until
            // at least one person joins
            if (group == null)
            {
                group = new Group();
                // new group: if can't add then delete
                if (!group.AddLeaderInvite(invitingPlayer))
                    return;

                if (!group.AddInvite(invitedPlayer))
                {
                    group.RemoveAllInvites();
                    return;
                }
            }
            else
            {
                // already existed group: if can't add then just leave
                if (!group.AddInvite(invitedPlayer))
                    return;
            }

            partyInvite = new PartyInvite();
            partyInvite.Initialize(invitingPlayer, packet.ProposedRoles, true);
            invitedPlayer.SendPacket(partyInvite);

            SendPartyResult(PartyOperation.Invite, invitedPlayer.GetName(), PartyResult.Ok);
        }

        [WorldPacketHandler(ClientOpcodes.PartyInviteResponse)]
        void HandlePartyInviteResponse(PartyInviteResponse packet)
        {
            Group group = GetPlayer().GetGroupInvite();
            if (group == null)
                return;

            if (packet.PartyIndex != 0 && group.GetGroupCategory() != (GroupCategory)packet.PartyIndex)
                return;

            if (packet.Accept)
            {
                // Remove player from invitees in any case
                group.RemoveInvite(GetPlayer());

                if (group.GetLeaderGUID() == GetPlayer().GetGUID())
                {
                    Log.outError(LogFilter.Network, "HandleGroupAcceptOpcode: player {0} ({1}) tried to accept an invite to his own group", GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                    return;
                }

                // Group is full
                if (group.IsFull())
                {
                    SendPartyResult(PartyOperation.Invite, "", PartyResult.GroupFull);
                    return;
                }

                Player leader = Global.ObjAccessor.FindPlayer(group.GetLeaderGUID());

                // Forming a new group, create it
                if (!group.IsCreated())
                {
                    // This can happen if the leader is zoning. To be removed once delayed actions for zoning are implemented
                    if (leader == null)
                    {
                        group.RemoveAllInvites();
                        return;
                    }

                    // If we're about to create a group there really should be a leader present
                    Cypher.Assert(leader != null);
                    group.RemoveInvite(leader);
                    group.Create(leader);
                    Global.GroupMgr.AddGroup(group);
                }

                // Everything is fine, do it, PLAYER'S GROUP IS SET IN ADDMEMBER!!!
                if (!group.AddMember(GetPlayer()))
                    return;

                group.BroadcastGroupUpdate();
            }
            else
            {
                // Remember leader if online (group will be invalid if group gets disbanded)
                Player leader = Global.ObjAccessor.FindPlayer(group.GetLeaderGUID());

                // uninvite, group can be deleted
                GetPlayer().UninviteFromGroup();

                if (leader == null || leader.GetSession() == null)
                    return;

                // report
                GroupDecline decline = new(GetPlayer().GetName());
                leader.SendPacket(decline);
            }
        }

        [WorldPacketHandler(ClientOpcodes.PartyUninvite)]
        void HandlePartyUninvite(PartyUninvite packet)
        {
            //can't uninvite yourself
            if (packet.TargetGUID == GetPlayer().GetGUID())
            {
                Log.outError(LogFilter.Network, "HandleGroupUninviteGuidOpcode: leader {0}({1}) tried to uninvite himself from the group.",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                return;
            }

            PartyResult res = GetPlayer().CanUninviteFromGroup(packet.TargetGUID, packet.PartyIndex);
            if (res != PartyResult.Ok)
            {
                SendPartyResult(PartyOperation.UnInvite, "", res);
                return;
            }

            Group grp = GetPlayer().GetGroup(packet.PartyIndex);
            // grp is checked already above in CanUninviteFromGroup()
            Cypher.Assert(grp != null);

            if (grp.IsMember(packet.TargetGUID))
            {
                Player.RemoveFromGroup(grp, packet.TargetGUID, RemoveMethod.Kick, GetPlayer().GetGUID(), packet.Reason);
                return;
            }
            Player player = grp.GetInvited(packet.TargetGUID);
            if (player != null)
            {
                player.UninviteFromGroup();
                return;
            }

            SendPartyResult(PartyOperation.UnInvite, "", PartyResult.TargetNotInGroupS);
        }

        [WorldPacketHandler(ClientOpcodes.SetPartyLeader, Processing = PacketProcessing.Inplace)]
        void HandleSetPartyLeader(SetPartyLeader packet)
        {
            Player player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetGUID);
            Group group = GetPlayer().GetGroup(packet.PartyIndex);

            if (group == null || player == null)
                return;

            if (!group.IsLeader(GetPlayer().GetGUID()) || player.GetGroup() != group)
                return;

            // Everything's fine, accepted.
            group.ChangeLeader(packet.TargetGUID);
            group.SendUpdate();
        }

        [WorldPacketHandler(ClientOpcodes.SetRole)]
        void HandleSetRole(SetRole packet)
        {
            RoleChangedInform roleChangedInform = new();

            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            byte oldRole = (byte)(group != null ? group.GetLfgRoles(packet.TargetGUID) : 0);
            if (oldRole == packet.Role)
                return;

            roleChangedInform.From = GetPlayer().GetGUID();
            roleChangedInform.ChangedUnit = packet.TargetGUID;
            roleChangedInform.OldRole = oldRole;
            roleChangedInform.NewRole = packet.Role;

            if (group != null)
            {
                roleChangedInform.PartyIndex = (byte)group.GetGroupCategory();
                group.BroadcastPacket(roleChangedInform, false);
                group.SetLfgRoles(packet.TargetGUID, (LfgRoles)packet.Role);
            }
            else
                SendPacket(roleChangedInform);
        }

        [WorldPacketHandler(ClientOpcodes.LeaveGroup)]
        void HandleLeaveGroup(LeaveGroup packet)
        {
            Group grp = GetPlayer().GetGroup(packet.PartyIndex);
            Group grpInvite = GetPlayer().GetGroupInvite();
            if (grp == null && grpInvite == null)
                return;

            if (GetPlayer().InBattleground())
            {
                SendPartyResult(PartyOperation.Invite, "", PartyResult.InviteRestricted);
                return;
            }

            /** error handling **/
            /********************/

            // everything's fine, do it
            if (grp != null)
            {
                SendPartyResult(PartyOperation.Leave, GetPlayer().GetName(), PartyResult.Ok);
                GetPlayer().RemoveFromGroup(RemoveMethod.Leave);
            }
            else if (grpInvite != null && grpInvite.GetLeaderGUID() == GetPlayer().GetGUID())
            { // pending group creation being cancelled
                SendPartyResult(PartyOperation.Leave, GetPlayer().GetName(), PartyResult.Ok);
                grpInvite.Disband();
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetLootMethod)]
        void HandleSetLootMethod(SetLootMethod packet)
        {
            // not allowed to change
            /*
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                 return;

            if (!group.IsLeader(GetPlayer().GetGUID()))
                return;

            if (group.IsLFGGroup())
                return;

            switch (packet.LootMethod)
            {
                case LootMethod.FreeForAll:
                case LootMethod.MasterLoot:
                case LootMethod.GroupLoot:
                case LootMethod.PersonalLoot:
                    break;
                default:
                    return;
            }

            if (packet.LootThreshold < ItemQuality.Uncommon || packet.LootThreshold > ItemQuality.Artifact)
                return;

            if (packet.LootMethod == LootMethod.MasterLoot && !group.IsMember(packet.LootMasterGUID))
                return;

            // everything's fine, do it
            group.SetLootMethod(packet.LootMethod);
            group.SetMasterLooterGuid(packet.LootMasterGUID);
            group.SetLootThreshold(packet.LootThreshold);
            group.SendUpdate();
            */
        }

        [WorldPacketHandler(ClientOpcodes.MinimapPing)]
        void HandleMinimapPing(MinimapPingClient packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            MinimapPing minimapPing = new();
            minimapPing.Sender = GetPlayer().GetGUID();
            minimapPing.PositionX = packet.PositionX;
            minimapPing.PositionY = packet.PositionY;
            group.BroadcastPacket(minimapPing, true, -1, GetPlayer().GetGUID());
        }

        [WorldPacketHandler(ClientOpcodes.RandomRoll)]
        void HandleRandomRoll(RandomRollClient packet)
        {
            if (packet.Min > packet.Max || packet.Max > 1000000)                // < 32768 for urand call
                return;

            GetPlayer().DoRandomRoll(packet.Min, packet.Max);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateRaidTarget)]
        void HandleUpdateRaidTarget(UpdateRaidTarget packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            if (packet.Symbol == -1)                  // target icon request
                group.SendTargetIconList(this);
            else                                        // target icon update
            {
                if (group.IsRaidGroup() && !group.IsLeader(GetPlayer().GetGUID()) && !group.IsAssistant(GetPlayer().GetGUID()))
                    return;

                if (packet.Target.IsPlayer())
                {
                    Player target = Global.ObjAccessor.FindConnectedPlayer(packet.Target);
                    if (target == null || target.IsHostileTo(GetPlayer()))
                        return;
                }

                group.SetTargetIcon((byte)packet.Symbol, packet.Target, GetPlayer().GetGUID());
            }
        }

        [WorldPacketHandler(ClientOpcodes.ConvertRaid)]
        void HandleConvertRaid(ConvertRaid packet)
        {
            Group group = GetPlayer().GetGroup();
            if (group == null)
                return;

            if (GetPlayer().InBattleground())
                return;

            // error handling
            if (!group.IsLeader(GetPlayer().GetGUID()) || group.GetMembersCount() < 2)
                return;

            // everything's fine, do it (is it 0 (PartyOperation.Invite) correct code)
            SendPartyResult(PartyOperation.Invite, "", PartyResult.Ok);

            // New 4.x: it is now possible to convert a raid to a group if member count is 5 or less
            if (packet.Raid)
                group.ConvertToRaid();
            else
                group.ConvertToGroup();
        }

        [WorldPacketHandler(ClientOpcodes.RequestPartyJoinUpdates)]
        void HandleRequestPartyJoinUpdates(RequestPartyJoinUpdates packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            group.SendTargetIconList(this);
            group.SendRaidMarkersChanged(this);
        }

        [WorldPacketHandler(ClientOpcodes.ChangeSubGroup, Processing = PacketProcessing.ThreadUnsafe)]
        void HandleChangeSubGroup(ChangeSubGroup packet)
        {
            // we will get correct for group here, so we don't have to check if group is BG raid
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            if (packet.NewSubGroup >= MapConst.MaxRaidSubGroups)
                return;

            ObjectGuid senderGuid = GetPlayer().GetGUID();
            if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
                return;

            if (!group.HasFreeSlotSubGroup(packet.NewSubGroup))
                return;

            group.ChangeMembersGroup(packet.TargetGUID, packet.NewSubGroup);
        }

        [WorldPacketHandler(ClientOpcodes.SwapSubGroups, Processing = PacketProcessing.ThreadUnsafe)]
        void HandleSwapSubGroups(SwapSubGroups packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            ObjectGuid senderGuid = GetPlayer().GetGUID();
            if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
                return;

            group.SwapMembersGroups(packet.FirstTarget, packet.SecondTarget);
        }

        [WorldPacketHandler(ClientOpcodes.SetAssistantLeader)]
        void HandleSetAssistantLeader(SetAssistantLeader packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            if (!group.IsLeader(GetPlayer().GetGUID()))
                return;

            group.SetGroupMemberFlag(packet.Target, packet.Apply, GroupMemberFlags.Assistant);
        }

        [WorldPacketHandler(ClientOpcodes.SetPartyAssignment)]
        void HandleSetPartyAssignment(SetPartyAssignment packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            ObjectGuid senderGuid = GetPlayer().GetGUID();
            if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
                return;

            switch ((GroupMemberAssignment)packet.Assignment)
            {
                case GroupMemberAssignment.MainAssist:
                    group.RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainAssist);
                    group.SetGroupMemberFlag(packet.Target, packet.Set, GroupMemberFlags.MainAssist);
                    break;
                case GroupMemberAssignment.MainTank:
                    group.RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainTank);           // Remove main assist flag from current if any.
                    group.SetGroupMemberFlag(packet.Target, packet.Set, GroupMemberFlags.MainTank);
                    break;
            }

            group.SendUpdate();
        }

        [WorldPacketHandler(ClientOpcodes.DoReadyCheck)]
        void HandleDoReadyCheckOpcode(DoReadyCheck packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            /** error handling **/
            if (!group.IsLeader(GetPlayer().GetGUID()) && !group.IsAssistant(GetPlayer().GetGUID()))
                return;

            // everything's fine, do it
            group.StartReadyCheck(GetPlayer().GetGUID(), TimeSpan.FromMilliseconds(MapConst.ReadycheckDuration));
        }

        [WorldPacketHandler(ClientOpcodes.ReadyCheckResponse, Processing = PacketProcessing.Inplace)]
        void HandleReadyCheckResponseOpcode(ReadyCheckResponseClient packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            // everything's fine, do it
            group.SetMemberReadyCheck(GetPlayer().GetGUID(), packet.IsReady);
        }

        [WorldPacketHandler(ClientOpcodes.RequestPartyMemberStats)]
        void HandleRequestPartyMemberStats(RequestPartyMemberStats packet)
        {
            PartyMemberFullState partyMemberStats = new();

            Player player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetGUID);
            if (player == null)
            {
                partyMemberStats.MemberGuid = packet.TargetGUID;
                partyMemberStats.MemberStats.Status = GroupMemberOnlineStatus.Offline;
            }
            else
                partyMemberStats.Initialize(player);

            SendPacket(partyMemberStats);
        }

        [WorldPacketHandler(ClientOpcodes.RequestRaidInfo)]
        void HandleRequestRaidInfo(RequestRaidInfo packet)
        {
            // every time the player checks the character screen
            GetPlayer().SendRaidInfo();
        }

        [WorldPacketHandler(ClientOpcodes.OptOutOfLoot, Processing = PacketProcessing.Inplace)]
        void HandleOptOutOfLoot(OptOutOfLoot packet)
        {
            // ignore if player not loaded
            if (GetPlayer() == null)                                        // needed because STATUS_AUTHED
            {
                if (packet.PassOnLoot)
                    Log.outError(LogFilter.Network, "CMSG_OPT_OUT_OF_LOOT value<>0 for not-loaded character!");
                return;
            }

            GetPlayer().SetPassOnGroupLoot(packet.PassOnLoot);
        }

        [WorldPacketHandler(ClientOpcodes.InitiateRolePoll)]
        void HandleInitiateRolePoll(InitiateRolePoll packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            ObjectGuid guid = GetPlayer().GetGUID();
            if (!group.IsLeader(guid) && !group.IsAssistant(guid))
                return;

            RolePollInform rolePollInform = new();
            rolePollInform.From = guid;
            rolePollInform.PartyIndex = (sbyte)group.GetGroupCategory();
            group.BroadcastPacket(rolePollInform, true);
        }

        [WorldPacketHandler(ClientOpcodes.SetEveryoneIsAssistant)]
        void HandleSetEveryoneIsAssistant(SetEveryoneIsAssistant packet)
        {
            Group group = GetPlayer().GetGroup(packet.PartyIndex);
            if (group == null)
                return;

            if (!group.IsLeader(GetPlayer().GetGUID()))
                return;

            group.SetEveryoneIsAssistant(packet.EveryoneIsAssistant);
        }

        [WorldPacketHandler(ClientOpcodes.ClearRaidMarker)]
        void HandleClearRaidMarker(ClearRaidMarker packet)
        {
            Group group = GetPlayer().GetGroup();
            if (group == null)
                return;

            if (group.IsRaidGroup() && !group.IsLeader(GetPlayer().GetGUID()) && !group.IsAssistant(GetPlayer().GetGUID()))
                return;

            group.DeleteRaidMarker(packet.MarkerId);
        }

        bool CanSendPing(Player player, PingSubjectType type, ref Group group)
        {
            if (type >= PingSubjectType.Max)
                return false;

            if (!player.GetSession().CanSpeak())
                return false;

            group = player.GetGroup();
            if (group == null)
                return false;

            if (group.IsRestrictPingsToAssistants() && !group.IsLeader(player.GetGUID()) && !group.IsAssistant(player.GetGUID()))
                return false;

            return true;
        }

        [WorldPacketHandler(ClientOpcodes.SetRestrictPingsToAssistants)]
        void HandleSetRestrictPingsToAssistants(SetRestrictPingsToAssistants setRestrictPingsToAssistants)
        {
            Group group = GetPlayer().GetGroup(setRestrictPingsToAssistants.PartyIndex);
            if (group == null)
                return;

            if (!group.IsLeader(GetPlayer().GetGUID()))
                return;

            group.SetRestrictPingsToAssistants(setRestrictPingsToAssistants.RestrictPingsToAssistants);
        }

        [WorldPacketHandler(ClientOpcodes.SendPingUnit)]
        void HandleSendPingUnit(SendPingUnit pingUnit)
        {
            Group group = null;
            if (!CanSendPing(_player, pingUnit.Type, ref group))
                return;

            Unit target = Global.ObjAccessor.GetUnit(_player, pingUnit.TargetGUID);
            if (target == null || !_player.HaveAtClient(target))
                return;

            ReceivePingUnit broadcastPingUnit = new();
            broadcastPingUnit.SenderGUID = _player.GetGUID();
            broadcastPingUnit.TargetGUID = pingUnit.TargetGUID;
            broadcastPingUnit.Type = pingUnit.Type;
            broadcastPingUnit.PinFrameID = pingUnit.PinFrameID;
            broadcastPingUnit.Write();

            for (GroupReference itr = group.GetFirstMember(); itr != null; itr = itr.Next())
            {
                Player member = itr.GetSource();
                if (_player == member || !_player.IsInMap(member))
                    continue;

                member.SendPacket(broadcastPingUnit);
            }
        }

        [WorldPacketHandler(ClientOpcodes.SendPingWorldPoint)]
        void HandleSendPingWorldPoint(SendPingWorldPoint pingWorldPoint)
        {
            Group group = null;
            if (!CanSendPing(_player, pingWorldPoint.Type, ref group))
                return;

            if (_player.GetMapId() != pingWorldPoint.MapID)
                return;

            ReceivePingWorldPoint broadcastPingWorldPoint = new();
            broadcastPingWorldPoint.SenderGUID = _player.GetGUID();
            broadcastPingWorldPoint.MapID = pingWorldPoint.MapID;
            broadcastPingWorldPoint.Point = pingWorldPoint.Point;
            broadcastPingWorldPoint.Type = pingWorldPoint.Type;
            broadcastPingWorldPoint.PinFrameID = pingWorldPoint.PinFrameID;
            broadcastPingWorldPoint.Write();

            for (GroupReference itr = group.GetFirstMember(); itr != null; itr = itr.Next())
            {
                Player member = itr.GetSource();
                if (_player == member || !_player.IsInMap(member))
                    continue;

                member.SendPacket(broadcastPingWorldPoint);
            }
        }
    }
}
