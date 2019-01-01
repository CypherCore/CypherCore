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
        static bool HandleLfgPlayerInfoCommand(StringArguments args, CommandHandler handler)
        {
            Player target = null;
            string playerName;
            ObjectGuid guid;
            if (!handler.extractPlayerTarget(args, out target, out guid, out playerName))
                return false;

            GetPlayerInfo(handler, target);
            return true;
        }

        [Command("group", RBACPermissions.CommandLfgGroup, true)]
        static bool HandleLfgGroupInfoCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player playerTarget = null;
            ObjectGuid guidTarget;
            string nameTarget;

            ObjectGuid parseGUID = ObjectGuid.Create(HighGuid.Player, args.NextUInt64());
            if (ObjectManager.GetPlayerNameByGUID(parseGUID, out nameTarget))
            {
                playerTarget = Global.ObjAccessor.FindPlayer(parseGUID);
                guidTarget = parseGUID;
            }
            else if (!handler.extractPlayerTarget(args, out playerTarget, out guidTarget, out nameTarget))
                return false;

            Group groupTarget = null;
            if (playerTarget)
                groupTarget = playerTarget.GetGroup();
            else
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
                stmt.AddValue(0, guidTarget.GetCounter());
                SQLResult resultGroup = DB.Characters.Query(stmt);
                if (!resultGroup.IsEmpty())
                    groupTarget = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
            }

            if (!groupTarget)
            {
                handler.SendSysMessage(CypherStrings.LfgNotInGroup, nameTarget);
                return false;
            }

            ObjectGuid guid = groupTarget.GetGUID();
            handler.SendSysMessage(CypherStrings.LfgGroupInfo, groupTarget.isLFGGroup(), Global.LFGMgr.GetState(guid), Global.LFGMgr.GetDungeon(guid));

            foreach (var slot in groupTarget.GetMemberSlots())
            {
                Player p = Global.ObjAccessor.FindPlayer(slot.guid);
                if (p)
                    GetPlayerInfo(handler, p);
                else
                    handler.SendSysMessage("{0} is offline.", slot.name);
            }

            return true;
        }

        [Command("options", RBACPermissions.CommandLfgOptions, true)]
        static bool HandleLfgOptionsCommand(StringArguments args, CommandHandler handler)
        {
            string str = args.NextString();
            int options = -1;
            if (!string.IsNullOrEmpty(str))
            {
                if (!int.TryParse(str, out options) || options < -1)
                    return false;
            }

            if (options != -1)
            {
                Global.LFGMgr.SetOptions((LfgOptions)options);
                handler.SendSysMessage(CypherStrings.LfgOptionsChanged);
            }
            handler.SendSysMessage(CypherStrings.LfgOptions, Global.LFGMgr.GetOptions());
            return true;
        }

        [Command("queue", RBACPermissions.CommandLfgQueue, true)]
        static bool HandleLfgQueueInfoCommand(StringArguments args, CommandHandler handler)
        {
            handler.SendSysMessage(Global.LFGMgr.DumpQueueInfo(args.NextBoolean()));
            return true;
        }

        [Command("clean", RBACPermissions.CommandLfgClean, true)]
        static bool HandleLfgCleanCommand(StringArguments args, CommandHandler handler)
        {
            handler.SendSysMessage(CypherStrings.LfgClean);
            Global.LFGMgr.Clean();
            return true;
        }

        static void GetPlayerInfo(CommandHandler handler, Player player)
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
