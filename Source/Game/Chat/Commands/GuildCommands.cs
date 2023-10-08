// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

            if (Global.GuildMgr.GetGuildByName(guildName) != null)
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
        static bool HandleGuildDeleteCommand(CommandHandler handler, QuotedString guildName)
        {
            if (guildName.IsEmpty())
                return false;

            Guild guild = Global.GuildMgr.GetGuildByName(guildName);
            if (guild == null)
                return false;

            guild.Disband();
            return true;
        }

        [Command("invite", RBACPermissions.CommandGuildInvite, true)]
        static bool HandleGuildInviteCommand(CommandHandler handler, PlayerIdentifier targetIdentifier, QuotedString guildName)
        {
            if (targetIdentifier == null)
                targetIdentifier = PlayerIdentifier.FromTargetOrSelf(handler);
            if (targetIdentifier == null)
                return false;

            if (guildName.IsEmpty())
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildByName(guildName);
            if (targetGuild == null)
                return false;

            targetGuild.AddMember(null, targetIdentifier.GetGUID());

            return true;
        }

        [Command("uninvite", RBACPermissions.CommandGuildUninvite, true)]
        static bool HandleGuildUninviteCommand(CommandHandler handler, PlayerIdentifier targetIdentifier)
        {
            if (targetIdentifier == null)
                targetIdentifier = PlayerIdentifier.FromTargetOrSelf(handler);
            if (targetIdentifier == null)
                return false;

            ulong guildId = targetIdentifier.IsConnected() ? targetIdentifier.GetConnectedPlayer().GetGuildId() : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(targetIdentifier.GetGUID());
            if (guildId == 0)
                return false;

            Guild targetGuild = Global.GuildMgr.GetGuildById(guildId);
            if (targetGuild == null)
                return false;

            targetGuild.DeleteMember(null, targetIdentifier.GetGUID(), false, true, true);
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
            if (targetGuild == null)
                return false;

            return targetGuild.ChangeMemberRank(null, player.GetGUID(), (GuildRankId)rank);
        }

        [Command("rename", RBACPermissions.CommandGuildRename, true)]
        static bool HandleGuildRenameCommand(CommandHandler handler, QuotedString oldGuildName, QuotedString newGuildName)
        {
            if (oldGuildName.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            if (newGuildName.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.InsertGuildName);
                return false;
            }

            Guild guild = Global.GuildMgr.GetGuildByName(oldGuildName);
            if (guild == null)
            {
                handler.SendSysMessage(CypherStrings.CommandCouldnotfind, oldGuildName);
                return false;
            }

            if (Global.GuildMgr.GetGuildByName(newGuildName) != null)
            {
                handler.SendSysMessage(CypherStrings.GuildRenameAlreadyExists, newGuildName);
                return false;
            }

            if (!guild.SetName(newGuildName))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            handler.SendSysMessage(CypherStrings.GuildRenameDone, oldGuildName, newGuildName);
            return true;
        }

        [Command("info", RBACPermissions.CommandGuildInfo, true)]
        static bool HandleGuildInfoCommand(CommandHandler handler, [OptionalArg][VariantArg<ulong, string>] dynamic guildIdentifier)
        {
            Guild guild = null;

            if (guildIdentifier != null)
            {
                if (guildIdentifier is ulong)
                    guild = Global.GuildMgr.GetGuildById(guildIdentifier);
                else
                    guild = Global.GuildMgr.GetGuildByName(guildIdentifier);
            }
            else
            {
                PlayerIdentifier target = PlayerIdentifier.FromTargetOrSelf(handler);
                if (target != null && target.IsConnected())
                    guild = target.GetConnectedPlayer().GetGuild();
            }

            if (guild == null)
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
