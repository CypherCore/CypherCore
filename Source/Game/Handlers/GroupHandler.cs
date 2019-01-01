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
using Game.Entities;
using Game.Groups;
using Game.Network;
using Game.Network.Packets;

namespace Game
{
    public partial class WorldSession
    {
        public void SendPartyResult(PartyOperation operation, string member, PartyResult res, uint val = 0)
        {
            PartyCommandResult packet = new PartyCommandResult();

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
            Player player = Global.ObjAccessor.FindPlayerByName(packet.TargetName);

            // no player
            if (!player)
            {
                SendPartyResult(PartyOperation.Invite, packet.TargetName, PartyResult.BadPlayerNameS);
                return;
            }

            // player trying to invite himself (most likely cheating)
            if (player == GetPlayer())
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.BadPlayerNameS);
                return;
            }

            // restrict invite to GMs
            if (!WorldConfig.GetBoolValue(WorldCfg.AllowGmGroup) && !GetPlayer().IsGameMaster() && player.IsGameMaster())
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.BadPlayerNameS);
                return;
            }

            // can't group with
            if (!GetPlayer().IsGameMaster() && !WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && GetPlayer().GetTeam() != player.GetTeam())
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.PlayerWrongFaction);
                return;
            }
            if (GetPlayer().GetInstanceId() != 0 && player.GetInstanceId() != 0 && GetPlayer().GetInstanceId() != player.GetInstanceId() && GetPlayer().GetMapId() == player.GetMapId())
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.TargetNotInInstanceS);
                return;
            }
            // just ignore us
            if (player.GetInstanceId() != 0 && player.GetDungeonDifficultyID() != GetPlayer().GetDungeonDifficultyID())
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.IgnoringYouS);
                return;
            }

            if (player.GetSocial().HasIgnore(GetPlayer().GetGUID()))
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.IgnoringYouS);
                return;
            }

            if (!player.GetSocial().HasFriend(GetPlayer().GetGUID()) && GetPlayer().getLevel() < WorldConfig.GetIntValue(WorldCfg.PartyLevelReq))
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.InviteRestricted);
                return;
            }

            Group group = GetPlayer().GetGroup();
            if (group && group.isBGGroup())
                group = GetPlayer().GetOriginalGroup();

            Group group2 = player.GetGroup();
            if (group2 && group2.isBGGroup())
                group2 = player.GetOriginalGroup();

            PartyInvite partyInvite;
            // player already in another group or invited
            if (group2 || player.GetGroupInvite())
            {
                SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.AlreadyInGroupS);

                if (group2)
                {
                    // tell the player that they were invited but it failed as they were already in a group
                    partyInvite = new PartyInvite();
                    partyInvite.Initialize(GetPlayer(), packet.ProposedRoles, false);
                    player.SendPacket(partyInvite);
                }

                return;
            }

            if (group)
            {
                // not have permissions for invite
                if (!group.IsLeader(GetPlayer().GetGUID()) && !group.IsAssistant(GetPlayer().GetGUID()))
                {
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
            if (!group)
            {
                group = new Group();
                // new group: if can't add then delete
                if (!group.AddLeaderInvite(GetPlayer()))
                    return;

                if (!group.AddInvite(player))
                {
                    group.RemoveAllInvites();
                    return;
                }
            }
            else
            {
                // already existed group: if can't add then just leave
                if (!group.AddInvite(player))
                    return;
            }

            partyInvite = new PartyInvite();
            partyInvite.Initialize(GetPlayer(), packet.ProposedRoles, true);
            player.SendPacket(partyInvite);

            SendPartyResult(PartyOperation.Invite, player.GetName(), PartyResult.Ok);
        }

        [WorldPacketHandler(ClientOpcodes.PartyInviteResponse)]
        void HandlePartyInviteResponse(PartyInviteResponse packet)
        {
            Group group = GetPlayer().GetGroupInvite();
            if (!group)
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
                    if (!leader)
                    {
                        group.RemoveAllInvites();
                        return;
                    }

                    // If we're about to create a group there really should be a leader present
                    Cypher.Assert(leader);
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

                if (!leader || leader.GetSession() == null)
                    return;

                // report
                GroupDecline decline = new GroupDecline(GetPlayer().GetName());
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

            PartyResult res = GetPlayer().CanUninviteFromGroup(packet.TargetGUID);
            if (res != PartyResult.Ok)
            {
                SendPartyResult(PartyOperation.UnInvite, "", res);
                return;
            }

            Group grp = GetPlayer().GetGroup();
            // grp is checked already above in CanUninviteFromGroup()
            Cypher.Assert(grp);

            if (grp.IsMember(packet.TargetGUID))
            {
                Player.RemoveFromGroup(grp, packet.TargetGUID, RemoveMethod.Kick, GetPlayer().GetGUID(), packet.Reason);
                return;
            }
            Player player = grp.GetInvited(packet.TargetGUID);
            if (player)
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
            Group group = GetPlayer().GetGroup();

            if (!group || !player)
                return;

            if (!group.IsLeader(GetPlayer().GetGUID()) || player.GetGroup() != group)
                return;

            // Everything's fine, accepted.
            group.ChangeLeader(packet.TargetGUID, packet.PartyIndex);
            group.SendUpdate();
        }

        [WorldPacketHandler(ClientOpcodes.SetRole, Processing = PacketProcessing.Inplace)]
        void HandleSetRole(SetRole packet)
        {
            RoleChangedInform roleChangedInform = new RoleChangedInform();

            Group group = GetPlayer().GetGroup();
            byte oldRole = (byte)(group ? group.GetLfgRoles(packet.TargetGUID) : 0);
            if (oldRole == packet.Role)
                return;

            roleChangedInform.PartyIndex = packet.PartyIndex;
            roleChangedInform.From = GetPlayer().GetGUID();
            roleChangedInform.ChangedUnit = packet.TargetGUID;
            roleChangedInform.OldRole = oldRole;
            roleChangedInform.NewRole = packet.Role;

            if (group)
            {
                group.BroadcastPacket(roleChangedInform, false);
                group.SetLfgRoles(packet.TargetGUID, (LfgRoles)packet.Role);
            }
            else
                SendPacket(roleChangedInform);
        }

        [WorldPacketHandler(ClientOpcodes.LeaveGroup)]
        void HandleLeaveGroup(LeaveGroup packet)
        {
            Group grp = GetPlayer().GetGroup();
            if (!grp)
                return;

            if (GetPlayer().InBattleground())
            {
                SendPartyResult(PartyOperation.Invite, "", PartyResult.InviteRestricted);
                return;
            }

            /** error handling **/
            /********************/

            // everything's fine, do it
            SendPartyResult(PartyOperation.Leave, GetPlayer().GetName(), PartyResult.Ok);

            GetPlayer().RemoveFromGroup(RemoveMethod.Leave);
        }

        [WorldPacketHandler(ClientOpcodes.SetLootMethod)]
        void HandleSetLootMethod(SetLootMethod packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            /** error handling **/
            if (!group.IsLeader(GetPlayer().GetGUID()))
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
        }

        [WorldPacketHandler(ClientOpcodes.LootRoll)]
        void HandleLootRoll(LootRoll packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            group.CountRollVote(GetPlayer().GetGUID(), packet.LootObj, (byte)(packet.LootListID - 1), packet.RollType);

            switch (packet.RollType)
            {
                case RollType.Need:
                    GetPlayer().UpdateCriteria(CriteriaTypes.RollNeed, 1);
                    break;
                case RollType.Greed:
                    GetPlayer().UpdateCriteria(CriteriaTypes.RollGreed, 1);
                    break;
            }
        }

        [WorldPacketHandler(ClientOpcodes.MinimapPing)]
        void HandleMinimapPing(MinimapPingClient packet)
        {
            if (!GetPlayer().GetGroup())
                return;

            MinimapPing minimapPing = new MinimapPing();
            minimapPing.Sender = GetPlayer().GetGUID();
            minimapPing.PositionX = packet.PositionX;
            minimapPing.PositionY = packet.PositionY;
            GetPlayer().GetGroup().BroadcastPacket(minimapPing, true, -1, GetPlayer().GetGUID());
        }

        [WorldPacketHandler(ClientOpcodes.RandomRoll)]
        void HandleRandomRoll(RandomRollClient packet)
        {
            if (packet.Min > packet.Max || packet.Max > 10000)                // < 32768 for urand call
                return;

            GetPlayer().DoRandomRoll(packet.Min, packet.Max);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateRaidTarget)]
        void HandleUpdateRaidTarget(UpdateRaidTarget packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            if (packet.Symbol == -1)                  // target icon request
                group.SendTargetIconList(this, packet.PartyIndex);
            else                                        // target icon update
            {
                if (group.isRaidGroup() && !group.IsLeader(GetPlayer().GetGUID()) && !group.IsAssistant(GetPlayer().GetGUID()))
                    return;

                if (packet.Target.IsPlayer())
                {
                    Player target = Global.ObjAccessor.FindConnectedPlayer(packet.Target);
                    if (!target || target.IsHostileTo(GetPlayer()))
                        return;
                }

                group.SetTargetIcon((byte)packet.Symbol, packet.Target, GetPlayer().GetGUID(), packet.PartyIndex);
            }
        }

        [WorldPacketHandler(ClientOpcodes.ConvertRaid)]
        void HandleConvertRaid(ConvertRaid packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
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
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            group.SendTargetIconList(this, packet.PartyIndex);
            group.SendRaidMarkersChanged(this, packet.PartyIndex);
        }

        [WorldPacketHandler(ClientOpcodes.ChangeSubGroup, Processing = PacketProcessing.ThreadUnsafe)]
        void HandleChangeSubGroup(ChangeSubGroup packet)
        {
            // we will get correct for group here, so we don't have to check if group is BG raid
            Group group = GetPlayer().GetGroup();
            if (!group)
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
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            ObjectGuid senderGuid = GetPlayer().GetGUID();
            if (!group.IsLeader(senderGuid) && !group.IsAssistant(senderGuid))
                return;

            group.SwapMembersGroups(packet.FirstTarget, packet.SecondTarget);
        }

        [WorldPacketHandler(ClientOpcodes.SetAssistantLeader)]
        void HandleSetAssistantLeader(SetAssistantLeader packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            if (!group.IsLeader(GetPlayer().GetGUID()))
                return;

            group.SetGroupMemberFlag(packet.Target, packet.Apply, GroupMemberFlags.Assistant);
        }

        [WorldPacketHandler(ClientOpcodes.SetPartyAssignment)]
        void HandleSetPartyAssignment(SetPartyAssignment packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
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
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            /** error handling **/
            if (!group.IsLeader(GetPlayer().GetGUID()) && !group.IsAssistant(GetPlayer().GetGUID()))
                return;

            // everything's fine, do it
            group.StartReadyCheck(GetPlayer().GetGUID(), packet.PartyIndex);
        }

        [WorldPacketHandler(ClientOpcodes.ReadyCheckResponse, Processing = PacketProcessing.Inplace)]
        void HandleReadyCheckResponseOpcode(ReadyCheckResponseClient packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            // everything's fine, do it
            group.SetMemberReadyCheck(GetPlayer().GetGUID(), packet.IsReady);
        }

        [WorldPacketHandler(ClientOpcodes.RequestPartyMemberStats)]
        void HandleRequestPartyMemberStats(RequestPartyMemberStats packet)
        {
            PartyMemberState partyMemberStats = new PartyMemberState();

            Player player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetGUID);
            if (!player)
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

        [WorldPacketHandler(ClientOpcodes.OptOutOfLoot)]
        void HandleOptOutOfLoot(OptOutOfLoot packet)
        {
            // ignore if player not loaded
            if (!GetPlayer())                                        // needed because STATUS_AUTHED
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
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            ObjectGuid guid = GetPlayer().GetGUID();
            if (!group.IsLeader(guid) && !group.IsAssistant(guid))
                return;

            RolePollInform rolePollInform = new RolePollInform();
            rolePollInform.From = guid;
            rolePollInform.PartyIndex = packet.PartyIndex;
            group.BroadcastPacket(rolePollInform, true);
        }

        [WorldPacketHandler(ClientOpcodes.SetEveryoneIsAssistant)]
        void HandleSetEveryoneIsAssistant(SetEveryoneIsAssistant packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            if (!group.IsLeader(GetPlayer().GetGUID()))
                return;

            group.SetEveryoneIsAssistant(packet.EveryoneIsAssistant);
        }

        [WorldPacketHandler(ClientOpcodes.ClearRaidMarker)]
        void HandleClearRaidMarker(ClearRaidMarker packet)
        {
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            if (group.isRaidGroup() && !group.IsLeader(GetPlayer().GetGUID()) && !group.IsAssistant(GetPlayer().GetGUID()))
                return;

            group.DeleteRaidMarker(packet.MarkerId);
        }
    }
}
