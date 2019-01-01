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
using Framework.IO;
using Game.Entities;
using Game.Guilds;

namespace Game.Chat
{
    [CommandGroup("guild", RBACPermissions.CommandGuild, true)]
    class GuildCommands
    {
        [Command("create", RBACPermissions.CommandGuildCreate, true)]
        static bool HandleGuildCreateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.extractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            string guildname = handler.extractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildname))
                return false;

            if (target.GetGuildId() != 0)
            {
                handler.SendSysMessage(CypherStrings.PlayerInGuild);
                return true;
            }

            Guild guild = new Guild();
            if (!guild.Create(target, guildname))
            {
                handler.SendSysMessage(CypherStrings.GuildNotCreated);
                return false;
            }

            Global.GuildMgr.AddGuild(guild);

            return true;
        }

        [Command("delete", RBACPermissions.CommandGuildDelete, true)]
        static bool HandleGuildDeleteCommand(StringArguments args, CommandHandler handler)
        {
            string guildName = handler.extractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildName))
                return false;

            Guild guild = Global.GuildMgr.GetGuildByName(guildName);
            if (guild == null)
                return false;

            guild.Disband();
            return true;
        }

        [Command("invite", RBACPermissions.CommandGuildInvite, true)]
        static bool HandleGuildInviteCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.extractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            string guildName = handler.extractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(guildName))
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildByName(guildName);
            if (targetGuild == null)
                return false;

            targetGuild.AddMember(null, target.GetGUID());

            return true;
        }

        [Command("uninvite", RBACPermissions.CommandGuildUninvite, true)]
        static bool HandleGuildUninviteCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid = ObjectGuid.Empty;
            if (!handler.extractPlayerTarget(args, out target, out targetGuid))
                return false;

            uint guildId = target != null ? target.GetGuildId() : Player.GetGuildIdFromDB(targetGuid);
            if (guildId == 0)
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildById(guildId);
            if (targetGuild == null)
                return false;

            targetGuild.DeleteMember(null, targetGuid, false, true, true);
            return true;
        }

        [Command("rank", RBACPermissions.CommandGuildRank, true)]
        static bool HandleGuildRankCommand(StringArguments args, CommandHandler handler)
        {
            string nameStr;
            string rankStr;
            handler.extractOptFirstArg(args, out nameStr, out rankStr);
            if (string.IsNullOrEmpty(rankStr))
                return false;

            Player target;
            ObjectGuid targetGuid;
            string target_name;
            if (!handler.extractPlayerTarget(new StringArguments(nameStr), out target, out targetGuid, out target_name))
                return false;

            ulong guildId = target ? target.GetGuildId() : Player.GetGuildIdFromDB(targetGuid);
            if (guildId == 0)
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildById(guildId);
            if (!targetGuild)
                return false;

            if (!byte.TryParse(rankStr, out byte newRank))
                return false;

            return targetGuild.ChangeMemberRank(null, targetGuid, newRank);
        }

        [Command("rename", RBACPermissions.CommandGuildRename, true)]
        static bool HandleGuildRenameCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string oldGuildStr = handler.extractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(oldGuildStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            string newGuildStr = handler.extractQuotedArg(args.NextString());
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
        static bool HandleGuildInfoCommand(StringArguments args, CommandHandler handler)
        {
            Guild guild = null;
            Player target = handler.getSelectedPlayerOrSelf();

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
            if (ObjectManager.GetPlayerNameByGUID(guild.GetLeaderGUID(), out guildMasterName))
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
