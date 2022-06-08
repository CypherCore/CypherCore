/*
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
using Game.DungeonFinding;
using Game.Entities;
using Game.Groups;

namespace Game.Chat
{
    [CommandGroup("lfg", RBACPermissions.CommandLfg, true)]
    class LFGCommands
    {
        [Command("player", RBACPermissions.CommandLfgPlayer, true)]
        static bool HandleLfgPlayerInfoCommand(CommandHandler handler, string playerName)
        {
            var player = PlayerIdentifier.ParseFromString(playerName);
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            Player target = player.GetConnectedPlayer();
            if (target != null)
            {
                PrintPlayerInfo(handler, target);
                return true;
            }

            return false;
        }

        [Command("group", RBACPermissions.CommandLfgGroup, true)]
        static bool HandleLfgGroupInfoCommand(CommandHandler handler, string playerName)
        {
            var player = PlayerIdentifier.ParseFromString(playerName);
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            Group groupTarget = null;
            Player target = player.GetConnectedPlayer();
            if (target != null)
                groupTarget = target.GetGroup();
            else
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
                stmt.AddValue(0, player.GetGUID().GetCounter());
                SQLResult resultGroup = DB.Characters.Query(stmt);
                if (!resultGroup.IsEmpty())
                    groupTarget = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
            }

            if (!groupTarget)
            {
                handler.SendSysMessage(CypherStrings.LfgNotInGroup, player.GetName());
                return false;
            }

            ObjectGuid guid = groupTarget.GetGUID();
            handler.SendSysMessage(CypherStrings.LfgGroupInfo, groupTarget.IsLFGGroup(), Global.LFGMgr.GetState(guid), Global.LFGMgr.GetDungeon(guid));

            foreach (var slot in groupTarget.GetMemberSlots())
            {
                Player p = Global.ObjAccessor.FindPlayer(slot.guid);
                if (p)
                    PrintPlayerInfo(handler, p);
                else
                    handler.SendSysMessage("{0} is offline.", slot.name);
            }

            return true;
        }

        [Command("options", RBACPermissions.CommandLfgOptions, true)]
        static bool HandleLfgOptionsCommand(CommandHandler handler, uint? optionsArg)
        {
            if (optionsArg.HasValue)
            {
                Global.LFGMgr.SetOptions((LfgOptions)optionsArg.Value);
                handler.SendSysMessage(CypherStrings.LfgOptionsChanged);
            }
            handler.SendSysMessage(CypherStrings.LfgOptions, Global.LFGMgr.GetOptions());
            return true;
        }

        [Command("queue", RBACPermissions.CommandLfgQueue, true)]
        static bool HandleLfgQueueInfoCommand(CommandHandler handler, bool full)
        {
            handler.SendSysMessage(Global.LFGMgr.DumpQueueInfo(full));
            return true;
        }

        [Command("clean", RBACPermissions.CommandLfgClean, true)]
        static bool HandleLfgCleanCommand(CommandHandler handler)
        {
            handler.SendSysMessage(CypherStrings.LfgClean);
            Global.LFGMgr.Clean();
            return true;
        }

        static void PrintPlayerInfo(CommandHandler handler, Player player)
        {
            if (!player)
                return;

            ObjectGuid guid = player.GetGUID();
            var dungeons = Global.LFGMgr.GetSelectedDungeons(guid);

            handler.SendSysMessage(CypherStrings.LfgPlayerInfo, player.GetName(), Global.LFGMgr.GetState(guid), dungeons.Count, LFGQueue.ConcatenateDungeons(dungeons),
                LFGQueue.GetRolesString(Global.LFGMgr.GetRoles(guid)));
        }
    }
}
