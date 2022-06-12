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
using Framework.IO;
using Game.Entities;
using Game.Guilds;

namespace Game.Chat
{
    [CommandGroup("guild")]
    class GuildCommands
    {
        [Command("create", RBACPermissions.CommandGuildCreate, true)]
        static bool HandleGuildCreateCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.ExtractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            string guildName = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildName))
                return false;

            if (target.GetGuildId() != 0)
            {
                handler.SendSysMessage(CypherStrings.PlayerInGuild);
                return false;
            }

            if (Global.GuildMgr.GetGuildByName(guildName))
            {
                handler.SendSysMessage(CypherStrings.GuildRenameAlreadyExists);
                return false;
            }

            if (Global.ObjectMgr.IsReservedName(guildName) || !ObjectManager.IsValidCharterName(guildName))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            Guild guild = new();
            if (!guild.Create(target, guildName))
            {
                handler.SendSysMessage(CypherStrings.GuildNotCreated);
                return false;
            }

            Global.GuildMgr.AddGuild(guild);

            return true;
        }

        [Command("delete", RBACPermissions.CommandGuildDelete, true)]
        static bool HandleGuildDeleteCommand(CommandHandler handler, StringArguments args)
        {
            string guildName = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildName))
                return false;

            Guild guild = Global.GuildMgr.GetGuildByName(guildName);
            if (guild == null)
                return false;

            guild.Disband();
            return true;
        }

        [Command("invite", RBACPermissions.CommandGuildInvite, true)]
        static bool HandleGuildInviteCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.ExtractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            string guildName = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildName))
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildByName(guildName);
            if (targetGuild == null)
                return false;

            targetGuild.AddMember(null, target.GetGUID());

            return true;
        }

        [Command("uninvite", RBACPermissions.CommandGuildUninvite, true)]
        static bool HandleGuildUninviteCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            ObjectGuid targetGuid;
            if (!handler.ExtractPlayerTarget(args, out target, out targetGuid))
                return false;

            ulong guildId = target != null ? target.GetGuildId() : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(targetGuid);
            if (guildId == 0)
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildById(guildId);
            if (targetGuild == null)
                return false;

            targetGuild.DeleteMember(null, targetGuid, false, true, true);
            return true;
        }

        [Command("rank", RBACPermissions.CommandGuildRank, true)]
        static bool HandleGuildRankCommand(CommandHandler handler, PlayerIdentifier player, byte rank)
        {
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            ulong guildId = player.IsConnected() ? player.GetConnectedPlayer().GetGuildId() : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(player.GetGUID());
            if (guildId == 0)
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildById(guildId);
            if (!targetGuild)
                return false;

            return targetGuild.ChangeMemberRank(null, player.GetGUID(), (GuildRankId)rank);
        }

        [Command("rename", RBACPermissions.CommandGuildRename, true)]
        static bool HandleGuildRenameCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            string oldGuildStr = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(oldGuildStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            string newGuildStr = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(newGuildStr))
            {
                handler.SendSysMessage(CypherStrings.InsertGuildName);
                return false;
            }

            Guild guild = Global.GuildMgr.GetGuildByName(oldGuildStr);
            if (!guild)
            {
                handler.SendSysMessage(CypherStrings.CommandCouldnotfind, oldGuildStr);
                return false;
            }

            if (Global.GuildMgr.GetGuildByName(newGuildStr))
            {
                handler.SendSysMessage(CypherStrings.GuildRenameAlreadyExists, newGuildStr);
                return false;
            }

            if (!guild.SetName(newGuildStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            handler.SendSysMessage(CypherStrings.GuildRenameDone, oldGuildStr, newGuildStr);
            return true;
        }

        [Command("info", RBACPermissions.CommandGuildInfo, true)]
        static bool HandleGuildInfoCommand(CommandHandler handler, StringArguments args)
        {
            Guild guild = null;
            Player target = handler.GetSelectedPlayerOrSelf();

            if (!args.Empty() && args[0] != '\0')
            {
                if (char.IsDigit(args[0]))
                    guild = Global.GuildMgr.GetGuildById(args.NextUInt64());
                else
                    guild = Global.GuildMgr.GetGuildByName(args.NextString());
            }
            else if (target)
                guild = target.GetGuild();

            if (!guild)
                return false;

            // Display Guild Information
            handler.SendSysMessage(CypherStrings.GuildInfoName, guild.GetName(), guild.GetId()); // Guild Id + Name

            string guildMasterName;
            if (Global.CharacterCacheStorage.GetCharacterNameByGuid(guild.GetLeaderGUID(), out guildMasterName))
                handler.SendSysMessage(CypherStrings.GuildInfoGuildMaster, guildMasterName, guild.GetLeaderGUID().ToString()); // Guild Master

            // Format creation date

            var createdDateTime = Time.UnixTimeToDateTime(guild.GetCreatedDate());
            handler.SendSysMessage(CypherStrings.GuildInfoCreationDate, createdDateTime.ToLongDateString()); // Creation Date
            handler.SendSysMessage(CypherStrings.GuildInfoMemberCount, guild.GetMembersCount()); // Number of Members
            handler.SendSysMessage(CypherStrings.GuildInfoBankGold, guild.GetBankMoney() / 100 / 100); // Bank Gold (in gold coins)
            handler.SendSysMessage(CypherStrings.GuildInfoLevel, guild.GetLevel()); // Level
            handler.SendSysMessage(CypherStrings.GuildInfoMotd, guild.GetMOTD()); // Message of the Day
            handler.SendSysMessage(CypherStrings.GuildInfoExtraInfo, guild.GetInfo()); // Extra Information
            return true;
        }
    }
}
