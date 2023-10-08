// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using System;

namespace Game.Chat
{
    class MessageCommands
    {
        [CommandNonGroup("nameannounce", RBACPermissions.CommandNameannounce, true)]
        static bool HandleNameAnnounceCommand(CommandHandler handler, Tail message)
        {
            if (message.IsEmpty())
                return false;

            string name = "Console";
            WorldSession session = handler.GetSession();
            if (session != null)
                name = session.GetPlayer().GetName();

            Global.WorldMgr.SendWorldText(CypherStrings.AnnounceColor, name, message);
            return true;
        }

        [CommandNonGroup("gmnameannounce", RBACPermissions.CommandGmnameannounce, true)]
        static bool HandleGMNameAnnounceCommand(CommandHandler handler, Tail message)
        {
            if (message.IsEmpty())
                return false;

            string name = "Console";
            WorldSession session = handler.GetSession();
            if (session != null)
                name = session.GetPlayer().GetName();

            Global.WorldMgr.SendGMText(CypherStrings.AnnounceColor, name, message);
            return true;
        }

        [CommandNonGroup("announce", RBACPermissions.CommandAnnounce, true)]
        static bool HandleAnnounceCommand(CommandHandler handler, Tail message)
        {
            if (message.IsEmpty())
                return false;

            Global.WorldMgr.SendServerMessage(ServerMessageType.String, handler.GetParsedString(CypherStrings.Systemmessage, message));
            return true;
        }

        [CommandNonGroup("gmannounce", RBACPermissions.CommandGmannounce, true)]
        static bool HandleGMAnnounceCommand(CommandHandler handler, Tail message)
        {
            if (message.IsEmpty())
                return false;

            Global.WorldMgr.SendGMText(CypherStrings.GmBroadcast, message);
            return true;
        }

        [CommandNonGroup("notify", RBACPermissions.CommandNotify, true)]
        static bool HandleNotifyCommand(CommandHandler handler, Tail message)
        {
            if (message.IsEmpty())
                return false;

            string str = handler.GetCypherString(CypherStrings.GlobalNotify);
            str += message;

            Global.WorldMgr.SendGlobalMessage(new PrintNotification(str));

            return true;
        }

        [CommandNonGroup("gmnotify", RBACPermissions.CommandGmnotify, true)]
        static bool HandleGMNotifyCommand(CommandHandler handler, Tail message)
        {
            if (message.IsEmpty())
                return false;

            string str = handler.GetCypherString(CypherStrings.GmNotify);
            str += message;

            Global.WorldMgr.SendGlobalGMMessage(new PrintNotification(str));

            return true;
        }

        [CommandNonGroup("whispers", RBACPermissions.CommandWhispers)]
        static bool HandleWhispersCommand(CommandHandler handler, bool? operationArg, [OptionalArg] string playerNameArg)
        {
            if (!operationArg.HasValue)
            {
                handler.SendSysMessage(CypherStrings.CommandWhisperaccepting, handler.GetSession().GetPlayer().IsAcceptWhispers() ? handler.GetCypherString(CypherStrings.On) : handler.GetCypherString(CypherStrings.Off));
                return true;
            }

            if (operationArg.HasValue)
            {
                handler.GetSession().GetPlayer().SetAcceptWhispers(true);
                handler.SendSysMessage(CypherStrings.CommandWhisperon);
                return true;
            }
            else
            {
                // Remove all players from the Gamemaster's whisper whitelist
                handler.GetSession().GetPlayer().ClearWhisperWhiteList();
                handler.GetSession().GetPlayer().SetAcceptWhispers(false);
                handler.SendSysMessage(CypherStrings.CommandWhisperoff);
                return true;
            }

            //todo fix me
            /*if (operationArg->holds_alternative < EXACT_SEQUENCE("remove") > ())
            {
                if (!playerNameArg)
                    return false;

                if (normalizePlayerName(*playerNameArg))
                {
                    if (Player * player = ObjectAccessor::FindPlayerByName(*playerNameArg))
                    {
                        handler->GetSession()->GetPlayer()->RemoveFromWhisperWhiteList(player->GetGUID());
                        handler->PSendSysMessage(LANG_COMMAND_WHISPEROFFPLAYER, playerNameArg->c_str());
                        return true;
                    }
                    else
                    {
                        handler->PSendSysMessage(LANG_PLAYER_NOT_FOUND, playerNameArg->c_str());
                        handler->SetSentErrorMessage(true);
                        return false;
                    }
                }
            }
            handler.SendSysMessage(CypherStrings.UseBol);
            return false;*/
        }        
    }

    [CommandGroup("channel")]
    class ChannelCommands
    {
        [CommandGroup("set")]
        class ChannelSetCommands
        {
            [Command("ownership", RBACPermissions.CommandChannelSetOwnership)]
            static bool HandleChannelSetOwnership(CommandHandler handler, string channelName, bool grantOwnership)
            {
                uint channelId = 0;
                foreach (var channelEntry in CliDB.ChatChannelsStorage.Values)
                {
                    if (channelEntry.Name[handler.GetSessionDbcLocale()].Equals(channelName, StringComparison.OrdinalIgnoreCase))
                    {
                        channelId = channelEntry.Id;
                        break;
                    }
                }

                AreaTableRecord zoneEntry = null;
                foreach (var entry in CliDB.AreaTableStorage.Values)
                {
                    if (entry.AreaName[handler.GetSessionDbcLocale()].Equals(channelName, StringComparison.OrdinalIgnoreCase))
                    {
                        zoneEntry = entry;
                        break;
                    }
                }

                Player player = handler.GetSession().GetPlayer();
                Channel channel = null;

                ChannelManager cMgr = ChannelManager.ForTeam(player.GetTeam());
                if (cMgr != null)
                    channel = cMgr.GetChannel(channelId, channelName, player, false, zoneEntry);

                if (grantOwnership)
                {
                    if (channel != null)
                        channel.SetOwnership(true);
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                    stmt.AddValue(0, 1);
                    stmt.AddValue(1, channelName);
                    DB.Characters.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.ChannelEnableOwnership, channelName);
                }
                else
                {
                    if (channel != null)
                        channel.SetOwnership(false);
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                    stmt.AddValue(0, 0);
                    stmt.AddValue(1, channelName);
                    DB.Characters.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.ChannelDisableOwnership, channelName);
                }

                return true;
            }
        }
    }
}
