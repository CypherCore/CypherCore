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
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using System;

namespace Game.Chat
{
    class MessageCommands
    {
        [CommandNonGroup("nameannounce", RBACPermissions.CommandNameannounce, true)]
        static bool HandleNameAnnounceCommand(CommandHandler handler, object[] args)
        {
            if (args.Length == 0)
                return false;

            string name = "Console";
            WorldSession session = handler.GetSession();
            if (session)
                name = session.GetPlayer().GetName();

            Global.WorldMgr.SendWorldText(CypherStrings.AnnounceColor, name, args);
            return true;
        }

        [CommandNonGroup("gmnameannounce", RBACPermissions.CommandGmnameannounce, true)]
        static bool HandleGMNameAnnounceCommand(CommandHandler handler, object[] args)
        {
            if (args.Length == 0)
                return false;

            string name = "Console";
            WorldSession session = handler.GetSession();
            if (session)
                name = session.GetPlayer().GetName();

            Global.WorldMgr.SendGMText(CypherStrings.AnnounceColor, name, args);
            return true;
        }

        [CommandNonGroup("announce", RBACPermissions.CommandAnnounce, true)]
        static bool HandleAnnounceCommand(CommandHandler handler, object[] args)
        {
            if (args.Length == 0)
                return false;

            string str = handler.GetParsedString(CypherStrings.Systemmessage, args);
            Global.WorldMgr.SendServerMessage(ServerMessageType.String, str);
            return true;
        }

        [CommandNonGroup("gmannounce", RBACPermissions.CommandGmannounce, true)]
        static bool HandleGMAnnounceCommand(CommandHandler handler, object[] args)
        {
            if (args.Length == 0)
                return false;

            Global.WorldMgr.SendGMText(CypherStrings.GmBroadcast, args);
            return true;
        }

        [CommandNonGroup("notify", RBACPermissions.CommandNotify, true)]
        static bool HandleNotifyCommand(CommandHandler handler, object[] args)
        {
            if (args.Length == 0)
                return false;

            string str = handler.GetCypherString(CypherStrings.GlobalNotify);
            foreach (string str2 in args)
                str += str2;

            Global.WorldMgr.SendGlobalMessage(new PrintNotification(str));

            return true;
        }

        [CommandNonGroup("gmnotify", RBACPermissions.CommandGmnotify, true)]
        static bool HandleGMNotifyCommand(CommandHandler handler, object[] args)
        {
            if (args.Length == 0)
                return false;

            string str = handler.GetCypherString(CypherStrings.GmNotify);
            foreach (string str2 in args)
                str += str2;

            Global.WorldMgr.SendGlobalGMMessage(new PrintNotification(str));

            return true;
        }

        [CommandNonGroup("whispers", RBACPermissions.CommandWhispers)]
        static bool HandleWhispersCommand(CommandHandler handler, bool? operationArg)
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
            }
            else
            {
                // Remove all players from the Gamemaster's whisper whitelist
                handler.GetSession().GetPlayer().ClearWhisperWhiteList();
                handler.GetSession().GetPlayer().SetAcceptWhispers(false);
                handler.SendSysMessage(CypherStrings.CommandWhisperoff);
            }

            return true;
        }        
    }

    [CommandGroup("channel", RBACPermissions.CommandChannel, true)]
    class ChannelCommands
    {
        [CommandGroup("set", RBACPermissions.CommandChannelSet, true)]
        class ChannelSetCommands
        {
            [Command("ownership", RBACPermissions.CommandChannelSetOwnership)]
            static bool HandleChannelSetOwnership(CommandHandler handler, string channelName, bool grantOwnership)
            {
                uint channelId = 0;
                foreach (var channelEntry in CliDB.ChatChannelsStorage.Values)
                {
                    if (channelEntry.Name[handler.GetSessionDbcLocale()].Equals(channelName))
                    {
                        channelId = channelEntry.Id;
                        break;
                    }
                }

                AreaTableRecord zoneEntry = null;
                foreach (var entry in CliDB.AreaTableStorage.Values)
                {
                    if (entry.AreaName[handler.GetSessionDbcLocale()].Equals(channelName))
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
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                    stmt.AddValue(0, 1);
                    stmt.AddValue(1, channelName);
                    DB.Characters.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.ChannelEnableOwnership, channelName);
                }
                else
                {
                    if (channel != null)
                        channel.SetOwnership(false);
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
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
