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
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System;

namespace Game.Chat
{
    class MessageCommands
    {
        [CommandNonGroup("nameannounce", RBACPermissions.CommandNameannounce, true)]
        static bool HandleNameAnnounceCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string name = "Console";
            WorldSession session = handler.GetSession();
            if (session)
                name = session.GetPlayer().GetName();

            Global.WorldMgr.SendWorldText(CypherStrings.AnnounceColor, name, args);
            return true;
        }

        [CommandNonGroup("gmnameannounce", RBACPermissions.CommandGmnameannounce, true)]
        static bool HandleGMNameAnnounceCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string name = "Console";
            WorldSession session = handler.GetSession();
            if (session)
                name = session.GetPlayer().GetName();

            Global.WorldMgr.SendGMText(CypherStrings.AnnounceColor, name, args);
            return true;
        }

        [CommandNonGroup("announce", RBACPermissions.CommandAnnounce, true)]
        static bool HandleAnnounceCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string str = handler.GetParsedString(CypherStrings.Systemmessage, args.NextString(""));
            Global.WorldMgr.SendServerMessage(ServerMessageType.String, str);
            return true;
        }

        [CommandNonGroup("gmannounce", RBACPermissions.CommandGmannounce, true)]
        static bool HandleGMAnnounceCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Global.WorldMgr.SendGMText(CypherStrings.GmBroadcast, args.NextString(""));
            return true;
        }

        [CommandNonGroup("notify", RBACPermissions.CommandNotify, true)]
        static bool HandleNotifyCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string str = handler.GetCypherString(CypherStrings.GlobalNotify);
            str += args.NextString("");

            Global.WorldMgr.SendGlobalMessage(new PrintNotification(str));

            return true;
        }

        [CommandNonGroup("gmnotify", RBACPermissions.CommandGmnotify, true)]
        static bool HandleGMNotifyCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string str = handler.GetCypherString(CypherStrings.GmNotify);
            str += args.NextString("");

            Global.WorldMgr.SendGlobalGMMessage(new PrintNotification(str));

            return true;
        }

        [CommandNonGroup("whispers", RBACPermissions.CommandWhispers)]
        static bool HandleWhispersCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CommandWhisperaccepting, handler.GetSession().GetPlayer().isAcceptWhispers() ? handler.GetCypherString(CypherStrings.On) : handler.GetCypherString(CypherStrings.Off));
                return true;
            }

            string argStr = args.NextString();
            // whisper on
            if (argStr == "on")
            {
                handler.GetSession().GetPlayer().SetAcceptWhispers(true);
                handler.SendSysMessage(CypherStrings.CommandWhisperon);
                return true;
            }

            // whisper off
            if (argStr == "off")
            {
                // Remove all players from the Gamemaster's whisper whitelist
                handler.GetSession().GetPlayer().ClearWhisperWhiteList();
                handler.GetSession().GetPlayer().SetAcceptWhispers(false);
                handler.SendSysMessage(CypherStrings.CommandWhisperoff);
                return true;
            }

            if (argStr == "remove")
            {
                string name = args.NextString();
                if (ObjectManager.NormalizePlayerName(ref name))
                {
                    Player player = Global.ObjAccessor.FindPlayerByName(name);
                    if (player)
                    {
                        handler.GetSession().GetPlayer().RemoveFromWhisperWhiteList(player.GetGUID());
                        handler.SendSysMessage(CypherStrings.CommandWhisperoffplayer, name);
                        return true;
                    }
                    else
                    {
                        handler.SendSysMessage(CypherStrings.PlayerNotFound, name);
                        return false;
                    }
                }
            }
            handler.SendSysMessage(CypherStrings.UseBol);
            return false;
        }        
    }

    [CommandGroup("channel", RBACPermissions.CommandChannel, true)]
    class ChannelCommands
    {
        [CommandGroup("set", RBACPermissions.CommandChannelSet, true)]
        class ChannelSetCommands
        {
            [Command("ownership", RBACPermissions.CommandChannelSetOwnership)]
            static bool HandleChannelSetOwnership(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                string channelStr = args.NextString();
                string argStr = args.NextString("");

                if (channelStr.IsEmpty() || argStr.IsEmpty())
                    return false;

                uint channelId = 0;
                foreach (var channelEntry in CliDB.ChatChannelsStorage.Values)
                {
                    if (channelEntry.Name[handler.GetSessionDbcLocale()].Equals(channelStr))
                    {
                        channelId = channelEntry.Id;
                        break;
                    }
                }

                AreaTableRecord zoneEntry = null;
                foreach (var entry in CliDB.AreaTableStorage.Values)
                {
                    if (entry.AreaName[handler.GetSessionDbcLocale()].Equals(channelStr))
                    {
                        zoneEntry = entry;
                        break;
                    }
                }

                Player player = handler.GetSession().GetPlayer();
                Channel channel = null;

                ChannelManager cMgr = ChannelManager.ForTeam(player.GetTeam());
                if (cMgr != null)
                    channel = cMgr.GetChannel(channelId, channelStr, player, false, zoneEntry);

                if (argStr.ToLower() == "on")
                {
                    if (channel != null)
                        channel.SetOwnership(true);
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                    stmt.AddValue(0, 1);
                    stmt.AddValue(1, channelStr);
                    DB.Characters.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.ChannelEnableOwnership, channelStr);
                }
                else if (argStr.ToLower() == "off")
                {
                    if (channel != null)
                        channel.SetOwnership(false);
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_OWNERSHIP);
                    stmt.AddValue(0, 0);
                    stmt.AddValue(1, channelStr);
                    DB.Characters.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.ChannelDisableOwnership, channelStr);
                }
                else
                    return false;

                return true;
            }
        }
    }
}
