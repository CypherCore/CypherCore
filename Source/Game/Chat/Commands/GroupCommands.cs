// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
    [CommandGroup("group")]
    class GroupCommands
    {
        [Command("disband", RBACPermissions.CommandGroupDisband)]
        static bool HandleGroupDisbandCommand(CommandHandler handler, string name)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out Group group, out _))
                return false;

            if (group == null)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            group.Disband();
            return true;
        }

        [Command("join", RBACPermissions.CommandGroupJoin)]
        static bool HandleGroupJoinCommand(CommandHandler handler, string playerNameGroup, string playerName)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(playerNameGroup, out Player playerSource, out Group groupSource, out _, true))
                return false;

            if (groupSource == null)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, playerSource.GetName());
                return false;
            }

            if (!handler.GetPlayerGroupAndGUIDByName(playerName, out Player playerTarget, out Group groupTarget, out _, true))
                return false;

            if (groupTarget != null || playerTarget.GetGroup() == groupSource)
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

        [Command("leader", RBACPermissions.CommandGroupLeader)]
        static bool HandleGroupLeaderCommand(CommandHandler handler, string name)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out Group group, out ObjectGuid guid))
                return false;

            if (group == null)
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

        [Command("level", RBACPermissions.CommandCharacterLevel, true)]
        static bool HandleGroupLevelCommand(CommandHandler handler, PlayerIdentifier player, short level)
        {
            if (level < 1)
                return false;

            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            Player target = player.GetConnectedPlayer();
            if (target == null)
                return false;

            Group groupTarget = target.GetGroup();
            if (groupTarget == null)
                return false;

            foreach (GroupReference groupRef in groupTarget.GetMembers())
            {
                target = groupRef.GetSource();
                uint oldlevel = target.GetLevel();

                if (level != oldlevel)
                {
                    target.SetLevel((uint)level);
                    target.InitTalentForLevel();
                    target.SetXP(0);
                }

                if (handler.NeedReportToTarget(target))
                {
                    if (oldlevel < level)
                        target.SendSysMessage(CypherStrings.YoursLevelUp, handler.GetNameLink(), level);
                    else                                                // if (oldlevel > newlevel)
                        target.SendSysMessage(CypherStrings.YoursLevelDown, handler.GetNameLink(), level);
                }
            }
            return true;
        }

        [Command("list", RBACPermissions.CommandGroupList)]
        static bool HandleGroupListCommand(CommandHandler handler, PlayerIdentifier target)
        {
            string zoneName = "<ERROR>";
            string onlineState = "Offline";

            // Next, we need a group. So we define a group variable.
            Group groupTarget = null;

            // We try to extract a group from an online player.
            if (target.IsConnected())
                groupTarget = target.GetConnectedPlayer().GetGroup();
            else
            {
                // If not, we extract it from the SQL.
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
                stmt.AddValue(0, target.GetGUID().GetCounter());
                SQLResult resultGroup = DB.Characters.Query(stmt);
                if (!resultGroup.IsEmpty())
                    groupTarget = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
            }

            // If both fails, players simply has no party. Return false.
            if (groupTarget == null)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, target.GetName());
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
                string flags = "";
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
                Player player = Global.ObjAccessor.FindPlayer(slot.guid);
                string phases = "";
                if (player != null && player.IsInWorld)
                {
                    // ... than, it prints information like "is online", where he is, etc...
                    onlineState = "online";
                    phases = PhasingHandler.FormatPhases(player.GetPhaseShift());

                    AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(player.GetAreaId());
                    if (area != null && area.HasFlag(AreaFlags.IsSubzone))
                    {
                        AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                        if (zone != null)
                            zoneName = zone.AreaName[handler.GetSessionDbcLocale()];
                    }
                }

                // Now we can print those informations for every single member of each group!
                handler.SendSysMessage(CypherStrings.GroupPlayerNameGuid, slot.name, onlineState,
                    zoneName, phases, slot.guid.ToString(), flags, LFGQueue.GetRolesString(slot.roles));
            }

            // And finish after every iterator is done.
            return true;
        }

        [Command("remove", RBACPermissions.CommandGroupRemove)]
        static bool HandleGroupRemoveCommand(CommandHandler handler, string name)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out Group group, out ObjectGuid guid))
                return false;

            if (group == null)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            group.RemoveMember(guid);
            return true;
        }

        [Command("repair", RBACPermissions.CommandRepairitems, true)]
        static bool HandleGroupRepairCommand(CommandHandler handler, PlayerIdentifier playerTarget)
        {
            if (playerTarget == null)
                playerTarget = PlayerIdentifier.FromTargetOrSelf(handler);
            if (playerTarget == null || !playerTarget.IsConnected())
                return false;

            Group groupTarget = playerTarget.GetConnectedPlayer().GetGroup();
            if (groupTarget == null)
                return false;

            foreach (GroupReference groupRef in groupTarget.GetMembers())
                groupRef.GetSource().DurabilityRepairAll(false, 0, false);

            return true;
        }

        [Command("revive", RBACPermissions.CommandRevive, true)]
        static bool HandleGroupReviveCommand(CommandHandler handler, PlayerIdentifier playerTarget)
        {
            if (playerTarget == null)
                playerTarget = PlayerIdentifier.FromTargetOrSelf(handler);
            if (playerTarget == null || !playerTarget.IsConnected())
                return false;

            Group groupTarget = playerTarget.GetConnectedPlayer().GetGroup();
            if (groupTarget == null)
                return false;

            foreach (GroupReference groupRef in groupTarget.GetMembers())
            {
                Player target = groupRef.GetSource();
                target.ResurrectPlayer(target.GetSession().HasPermission(RBACPermissions.ResurrectWithFullHps) ? 1.0f : 0.5f);
                target.SpawnCorpseBones();
                target.SaveToDB();
            }

            return true;
        }

        [Command("summon", RBACPermissions.CommandGroupSummon)]
        static bool HandleGroupSummonCommand(CommandHandler handler, PlayerIdentifier playerTarget)
        {
            if (playerTarget == null)
                playerTarget = PlayerIdentifier.FromTargetOrSelf(handler);
            if (playerTarget == null || !playerTarget.IsConnected())
                return false;

            Player target = playerTarget.GetConnectedPlayer();

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            Group group = target.GetGroup();

            string nameLink = handler.GetNameLink(target);

            if (group == null)
            {
                handler.SendSysMessage(CypherStrings.NotInGroup, nameLink);
                return false;
            }

            Player gmPlayer = handler.GetSession().GetPlayer();
            Map gmMap = gmPlayer.GetMap();
            bool toInstance = gmMap.Instanceable();
            bool onlyLocalSummon = false;

            // make sure people end up on our instance of the map, disallow far summon if intended destination is different from actual destination
            // note: we could probably relax this further by checking permanent saves and the like, but eh
            // :close enough:
            if (toInstance)
            {
                Player groupLeader = Global.ObjAccessor.GetPlayer(gmMap, group.GetLeaderGUID());
                if (groupLeader == null || (groupLeader.GetMapId() != gmMap.GetId()) || (groupLeader.GetInstanceId() != gmMap.GetInstanceId()))
                {
                    handler.SendSysMessage(CypherStrings.PartialGroupSummon);
                    onlyLocalSummon = true;
                }
            }

            foreach (GroupReference groupRef in group.GetMembers())
            {
                Player player = groupRef.GetSource();
                if (player == gmPlayer || player.GetSession() == null)
                    continue;

                // check online security
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                    continue;

                string plNameLink = handler.GetNameLink(player);

                if (player.IsBeingTeleported())
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, plNameLink);
                    continue;
                }

                if (toInstance)
                {
                    Map playerMap = player.GetMap();

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
                    player.FinishTaxiFlight();
                else
                    player.SaveRecallPosition(); // save only in non-flight case

                // before GM
                float x, y, z;
                gmPlayer.GetClosePoint(out x, out y, out z, player.GetCombatReach());
                player.TeleportTo(gmPlayer.GetMapId(), x, y, z, player.GetOrientation(), 0, gmPlayer.GetInstanceId());
            }

            return true;
        }

        [CommandGroup("set")]
        class GroupSetCommands
        {
            [Command("assistant", RBACPermissions.CommandGroupAssistant)]
            static bool HandleGroupSetAssistantCommand(CommandHandler handler, string name)
            {
                return GroupFlagCommand(name, handler, GroupMemberFlags.Assistant);
            }

            [Command("leader", RBACPermissions.CommandGroupLeader)]
            static bool HandleGroupSetLeaderCommand(CommandHandler handler, string name)
            {
                return HandleGroupLeaderCommand(handler, name);
            }

            [Command("mainassist", RBACPermissions.CommandGroupMainassist)]
            static bool HandleGroupSetMainAssistCommand(CommandHandler handler, string name)
            {
                return GroupFlagCommand(name, handler, GroupMemberFlags.MainAssist);
            }

            [Command("maintank", RBACPermissions.CommandGroupMaintank)]
            static bool HandleGroupSetMainTankCommand(CommandHandler handler, string name)
            {
                return GroupFlagCommand(name, handler, GroupMemberFlags.MainTank);
            }

            static bool GroupFlagCommand(string name, CommandHandler handler, GroupMemberFlags flag)
            {
                if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out Group group, out ObjectGuid guid))
                    return false;

                if (group == null)
                {
                    handler.SendSysMessage(CypherStrings.NotInGroup, player.GetName());
                    return false;
                }

                if (!group.IsRaidGroup())
                {
                    handler.SendSysMessage(CypherStrings.GroupNotInRaidGroup, player.GetName());
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
                    handler.SendSysMessage(CypherStrings.GroupRoleChanged, player.GetName(), "no longer", flag);
                }
                else
                {
                    group.SetGroupMemberFlag(guid, true, flag);
                    handler.SendSysMessage(CypherStrings.GroupRoleChanged, player.GetName(), "now", flag);
                }
                return true;
            }
        }
    }
}
