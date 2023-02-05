// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DungeonFinding;
using Game.Entities;
using Game.Groups;
using System;

namespace Game.Chat
{
    [CommandGroup("lfg")]
    class LFGCommands
    {
        [Command("player", RBACPermissions.CommandLfgPlayer, true)]
        static bool HandleLfgPlayerInfoCommand(CommandHandler handler, PlayerIdentifier player)
        {
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
        static bool HandleLfgGroupInfoCommand(CommandHandler handler, PlayerIdentifier player)
        {
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
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
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
        static bool HandleLfgQueueInfoCommand(CommandHandler handler, string full)
        {
            handler.SendSysMessage(Global.LFGMgr.DumpQueueInfo(!full.IsEmpty()));
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
