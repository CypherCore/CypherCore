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
    [CommandGroup("guild", RBACPermissions.CommandGuild, true)]
    internal class GuildCommands
    {
        [Command("create", RBACPermissions.CommandGuildCreate, true)]
        private static bool HandleGuildCreateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.ExtractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            var guildname = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildname))
                return false;

            if (target.GetGuildId() != 0)
            {
                handler.SendSysMessage(CypherStrings.PlayerInGuild);
                return true;
            }

            var guild = new Guild();
            if (!guild.Create(target, guildname))
            {
                handler.SendSysMessage(CypherStrings.GuildNotCreated);
                return false;
            }

            Global.GuildMgr.AddGuild(guild);

            return true;
        }

        [Command("delete", RBACPermissions.CommandGuildDelete, true)]
        private static bool HandleGuildDeleteCommand(StringArguments args, CommandHandler handler)
        {
            var guildName = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildName))
                return false;

            var guild = Global.GuildMgr.GetGuildByName(guildName);
            if (guild == null)
                return false;

            guild.Disband();
            return true;
        }

        [Command("invite", RBACPermissions.CommandGuildInvite, true)]
        private static bool HandleGuildInviteCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.ExtractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            var guildName = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildName))
                return false;

            var targetGuild = Global.GuildMgr.GetGuildByName(guildName);
            if (targetGuild == null)
                return false;

            targetGuild.AddMember(null, target.GetGUID());

            return true;
        }

        [Command("uninvite", RBACPermissions.CommandGuildUninvite, true)]
        private static bool HandleGuildUninviteCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            if (!handler.ExtractPlayerTarget(args, out target, out targetGuid))
                return false;

            var guildId = target != null ? target.GetGuildId() : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(targetGuid);
            if (guildId == 0)
                return false;

            var targetGuild = Global.GuildMgr.GetGuildById(guildId);
            if (targetGuild == null)
                return false;

            targetGuild.DeleteMember(null, targetGuid, false, true, true);
            return true;
        }

        [Command("rank", RBACPermissions.CommandGuildRank, true)]
        private static bool HandleGuildRankCommand(StringArguments args, CommandHandler handler)
        {
            string nameStr;
            string rankStr;
            handler.ExtractOptFirstArg(args, out nameStr, out rankStr);
            if (string.IsNullOrEmpty(rankStr))
                return false;

            Player target;
            ObjectGuid targetGuid;
            if (!handler.ExtractPlayerTarget(new StringArguments(nameStr), out target, out targetGuid, out _))
                return false;

            var guildId = target ? target.GetGuildId() : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(targetGuid);
            if (guildId == 0)
                return false;

            var targetGuild = Global.GuildMgr.GetGuildById(guildId);
            if (!targetGuild)
                return false;

            if (!byte.TryParse(rankStr, out var newRank))
                return false;

            return targetGuild.ChangeMemberRank(null, targetGuid, newRank);
        }

        [Command("rename", RBACPermissions.CommandGuildRename, true)]
        private static bool HandleGuildRenameCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var oldGuildStr = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(oldGuildStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            var newGuildStr = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(newGuildStr))
            {
                handler.SendSysMessage(CypherStrings.InsertGuildName);
                return false;
            }

            var guild = Global.GuildMgr.GetGuildByName(oldGuildStr);
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
        private static bool HandleGuildInfoCommand(StringArguments args, CommandHandler handler)
        {
            Guild guild = null;
            var target = handler.GetSelectedPlayerOrSelf();

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
