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
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.DungeonFinding;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("group", RBACPermissions.CommandGroup)]
    internal class GroupCommands
    {
        // Summon group of player
        [Command("summon", RBACPermissions.CommandGroupSummon)]
        private static bool HandleGroupSummonCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            if (!handler.ExtractPlayerTarget(args, out target))
                return false;

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            var group = target.GetGroup();

            var nameLink = handler.GetNameLink(target);

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.NotInGroup, nameLink);
                return false;
            }

            var gmPlayer = handler.GetSession().GetPlayer();
            var gmMap = gmPlayer.GetMap();
            var toInstance = gmMap.Instanceable();
            var onlyLocalSummon = false;

            // make sure people end up on our instance of the map, disallow far summon if intended destination is different from actual destination
            // note: we could probably relax this further by checking permanent saves and the like, but eh
            // :close enough:
            if (toInstance)
            {
                var groupLeader = Global.ObjAccessor.GetPlayer(gmMap, group.GetLeaderGUID());
                if (!groupLeader || (groupLeader.GetMapId() != gmMap.GetId()) || (groupLeader.GetInstanceId() != gmMap.GetInstanceId()))
                {
                    handler.SendSysMessage(CypherStrings.PartialGroupSummon);
                    onlyLocalSummon = true;
                }
            }

            for (var refe = group.GetFirstMember(); refe != null; refe = refe.Next())
            {
                var player = refe.GetSource();

                if (!player || player == gmPlayer || player.GetSession() == null)
                    continue;

                // check online security
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                    continue;

                var plNameLink = handler.GetNameLink(player);

                if (player.IsBeingTeleported())
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, plNameLink);
                    continue;
                }

                if (toInstance)
                {
                    var playerMap = player.GetMap();

                    if ((onlyLocalSummon || (playerMap.Instanceable() && playerMap.GetId() == gmMap.GetId())) && // either no far summon allowed or we're in the same map as player (no map switch)
                        ((playerMap.GetId() != gmMap.GetId()) || (playerMap.GetInstanceId() != gmMap.GetInstanceId()))) // so we need to be in the same map and instance of the map, otherwise skip
                    {
                        // cannot summon from instance to instance
                        handler.SendSysMessage(CypherStrings.CannotSummonInstInst, plNameLink);
                        continue;
                    }
                }

                handler.SendSysMessage(CypherStrings.Summoning, plNameLink, "");
                if (handler.NeedReportToTarget(player))
                    player.SendSysMessage(CypherStrings.SummonedBy, handler.GetNameLink());

                // stop flight if need
                if (player.IsInFlight())
                {
                    player.GetMotionMaster().MovementExpired();
                    player.CleanupAfterTaxiFlight();
                }
                // save only in non-flight case
                else
                    player.SaveRecallPosition();

                // before GM
                float x, y, z;
                gmPlayer.GetClosePoint(out x, out y, out z, player.GetCombatReach());
                player.TeleportTo(gmPlayer.GetMapId(), x, y, z, player.GetOrientation());
            }

            return true;
        }

        [Command("leader", RBACPermissions.CommandGroupLeader)]
        private static bool HandleGroupLeaderCommand(StringArguments args, CommandHandler handler)
        {
            Player player;
            Group group;
            ObjectGuid guid;

            if (!handler.GetPlayerGroupAndGUIDByName(args.NextString(), out player, out group, out guid))
                return false;

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            if (group.GetLeaderGUID() != guid)
            {
                group.ChangeLeader(guid);
                group.SendUpdate();
            }

            return true;
        }

        [Command("disband", RBACPermissions.CommandGroupDisband)]
        private static bool HandleGroupDisbandCommand(StringArguments args, CommandHandler handler)
        {
            Player player;
            Group group;
            var nameStr = args.NextString();

            if (!handler.GetPlayerGroupAndGUIDByName(nameStr, out player, out group, out _))
                return false;

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            group.Disband();
            return true;
        }

        [Command("remove", RBACPermissions.CommandGroupRemove)]
        private static bool HandleGroupRemoveCommand(StringArguments args, CommandHandler handler)
        {
            Player player;
            Group group;
            ObjectGuid guid;
            var nameStr = args.NextString();

            if (!handler.GetPlayerGroupAndGUIDByName(nameStr, out player, out group, out guid))
                return false;

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            group.RemoveMember(guid);
            return true;
        }

        [Command("join", RBACPermissions.CommandGroupJoin)]
        private static bool HandleGroupJoinCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player playerSource;
            Player playerTarget;
            Group groupSource;
            Group groupTarget;
            var nameplgrStr = args.NextString();
            var nameplStr = args.NextString();

            if (!handler.GetPlayerGroupAndGUIDByName(nameplgrStr, out playerSource, out groupSource, out _, true))
                return false;

            if (!groupSource)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, playerSource.GetName());
                return false;
            }

            if (!handler.GetPlayerGroupAndGUIDByName(nameplStr, out playerTarget, out groupTarget, out _, true))
                return false;

            if (groupTarget || playerTarget.GetGroup() == groupSource)
            {
                handler.SendSysMessage(CypherStrings.GroupAlreadyInGroup, playerTarget.GetName());
                return false;
            }

            if (groupSource.IsFull())
            {
                handler.SendSysMessage(CypherStrings.GroupFull);
                return false;
            }

            groupSource.AddMember(playerTarget);
            groupSource.BroadcastGroupUpdate();
            handler.SendSysMessage(CypherStrings.GroupPlayerJoined, playerTarget.GetName(), playerSource.GetName());
            return true;
        }

        [Command("list", RBACPermissions.CommandGroupList)]
        private static bool HandleGroupListCommand(StringArguments args, CommandHandler handler)
        {
            // Get ALL the variables!
            Player playerTarget;
            ObjectGuid guidTarget;
            string nameTarget;
            var zoneName = "";
            string onlineState;

            // Parse the guid to uint32...
            var parseGUID = ObjectGuid.Create(HighGuid.Player, args.NextUInt64());

            // ... and try to extract a player out of it.
            if (Global.CharacterCacheStorage.GetCharacterNameByGuid(parseGUID, out nameTarget))
            {
                playerTarget = Global.ObjAccessor.FindPlayer(parseGUID);
                guidTarget = parseGUID;
            }
            // If not, we return false and end right away.
            else if (!handler.ExtractPlayerTarget(args, out playerTarget, out guidTarget, out nameTarget))
                return false;

            // Next, we need a group. So we define a group variable.
            Group groupTarget = null;

            // We try to extract a group from an online player.
            if (playerTarget)
                groupTarget = playerTarget.GetGroup();

            // If not, we extract it from the SQL.
            if (!groupTarget)
            {
                var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
                stmt.AddValue(0, guidTarget.GetCounter());
                var resultGroup = DB.Characters.Query(stmt);
                if (!resultGroup.IsEmpty())
                    groupTarget = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
            }

            // If both fails, players simply has no party. Return false.
            if (!groupTarget)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, nameTarget);
                return false;
            }

            // We get the group members after successfully detecting a group.
            var members = groupTarget.GetMemberSlots();

            // To avoid a cluster fuck, namely trying multiple queries to simply get a group member count...
            handler.SendSysMessage(CypherStrings.GroupType, (groupTarget.IsRaidGroup() ? "raid" : "party"), members.Count);
            // ... we simply move the group type and member count print after retrieving the slots and simply output it's size.

            // While rather dirty codestyle-wise, it saves space (if only a little). For each member, we look several informations up.
            foreach (var slot in members)
            {
                // Check for given flag and assign it to that iterator
                var flags = "";
                if (slot.flags.HasAnyFlag(GroupMemberFlags.Assistant))
                    flags = "Assistant";

                if (slot.flags.HasAnyFlag(GroupMemberFlags.MainTank))
                {
                    if (!string.IsNullOrEmpty(flags))
                        flags += ", ";
                    flags += "MainTank";
                }

                if (slot.flags.HasAnyFlag(GroupMemberFlags.MainAssist))
                {
                    if (!string.IsNullOrEmpty(flags))
                        flags += ", ";
                    flags += "MainAssist";
                }

                if (string.IsNullOrEmpty(flags))
                    flags = "None";

                // Check if iterator is online. If is...
                var p = Global.ObjAccessor.FindPlayer(slot.guid);
                var phases = "";
                if (p && p.IsInWorld)
                {
                    // ... than, it prints information like "is online", where he is, etc...
                    onlineState = "online";
                    phases = PhasingHandler.FormatPhases(p.GetPhaseShift());

                    var area = CliDB.AreaTableStorage.LookupByKey(p.GetAreaId());
                    if (area != null)
                    {
                        var zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                        if (zone != null)
                            zoneName = zone.AreaName[handler.GetSessionDbcLocale()];
                    }
                }
                else
                {
                    // ... else, everything is set to offline or neutral values.
                    zoneName = "<ERROR>";
                    onlineState = "Offline";
                }

                // Now we can print those informations for every single member of each group!
                handler.SendSysMessage(CypherStrings.GroupPlayerNameGuid, slot.name, onlineState,
                    zoneName, phases, slot.guid.ToString(), flags, LFGQueue.GetRolesString(slot.roles));
            }

            // And finish after every iterator is done.
            return true;
        }

        [CommandGroup("set", RBACPermissions.CommandGroupSet)]
        private class GroupSetCommands
        {
            [Command("assistant", RBACPermissions.CommandGroupAssistant)]
            private static bool HandleGroupSetAssistantCommand(StringArguments args, CommandHandler handler)
            {
                return GroupFlagCommand(args, handler, GroupMemberFlags.Assistant, "Assistant");
            }

            [Command("leader", RBACPermissions.CommandGroupLeader)]
            private static bool HandleGroupSetLeaderCommand(StringArguments args, CommandHandler handler)
            {
                return HandleGroupLeaderCommand(args, handler);
            }

            [Command("mainassist", RBACPermissions.CommandGroupMainassist)]
            private static bool HandleGroupSetMainAssistCommand(StringArguments args, CommandHandler handler)
            {
                return GroupFlagCommand(args, handler, GroupMemberFlags.MainAssist, "Main Assist");
            }

            [Command("maintank", RBACPermissions.CommandGroupMaintank)]
            private static bool HandleGroupSetMainTankCommand(StringArguments args, CommandHandler handler)
            {
                return GroupFlagCommand(args, handler, GroupMemberFlags.MainTank, "Main Tank");
            }

            private static bool GroupFlagCommand(StringArguments args, CommandHandler handler, GroupMemberFlags flag, string what)
            {
                Player player;
                Group group;
                ObjectGuid guid;

                if (!handler.GetPlayerGroupAndGUIDByName(args.NextString(), out player, out group, out guid))
                    return false;

                if (!group)
                {
                    handler.SendSysMessage(CypherStrings.NotInGroup, player.GetName());
                    return false;
                }

                if (!group.IsRaidGroup())
                {
                    handler.SendSysMessage(CypherStrings.NotInRaidGroup, player.GetName());
                    return false;
                }

                if (flag == GroupMemberFlags.Assistant && group.IsLeader(guid))
                {
                    handler.SendSysMessage(CypherStrings.LeaderCannotBeAssistant, player.GetName());
                    return false;
                }

                if (group.GetMemberFlags(guid).HasAnyFlag(flag))
                {
                    group.SetGroupMemberFlag(guid, false, flag);
                    handler.SendSysMessage(CypherStrings.GroupRoleChanged, player.GetName(), "no longer", what);
                }
                else
                {
                    group.SetGroupMemberFlag(guid, true, flag);
                    handler.SendSysMessage(CypherStrings.GroupRoleChanged, player.GetName(), "now", what);
                }
                return true;
            }
        }
    }
}
