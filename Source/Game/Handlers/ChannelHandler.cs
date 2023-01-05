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
using Game.Chat;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.ChatJoinChannel)]
        void HandleJoinChannel(JoinChannel packet)
        {
            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(GetPlayer().GetZoneId());
            if (packet.ChatChannelId != 0)
            {
                ChatChannelsRecord channel = CliDB.ChatChannelsStorage.LookupByKey(packet.ChatChannelId);
                if (channel == null)
                    return;

                if (zone == null || !GetPlayer().CanJoinConstantChannelInZone(channel, zone))
                    return;
            }

            ChannelManager cMgr = ChannelManager.ForTeam(GetPlayer().GetTeam());
            if (cMgr == null)
                return;

            if (packet.ChatChannelId != 0)
            { // system channel
                Channel channel = cMgr.GetSystemChannel((uint)packet.ChatChannelId, zone);
                if (channel != null)
                    channel.JoinChannel(GetPlayer());
            }
            else
            { // custom channel
                if (packet.ChannelName.IsEmpty() || Char.IsDigit(packet.ChannelName[0]))
                {
                    ChannelNotify channelNotify = new();
                    channelNotify.Type = ChatNotify.InvalidNameNotice;
                    channelNotify.Channel = packet.ChannelName;
                    SendPacket(channelNotify);
                    return;
                }

                if (packet.Password.Length > 127)
                {
                    Log.outError(LogFilter.Network, $"Player {GetPlayer().GetGUID()} tried to create a channel with a password more than {127} characters long - blocked");
                    return;
                }
                if (!DisallowHyperlinksAndMaybeKick(packet.ChannelName))
                    return;

                Channel channel = cMgr.GetCustomChannel(packet.ChannelName);
                if (channel != null)
                    channel.JoinChannel(GetPlayer(), packet.Password);
                else
                {
                    channel = cMgr.CreateCustomChannel(packet.ChannelName);
                    if (channel != null)
                    {
                        channel.SetPassword(packet.Password);
                        channel.JoinChannel(GetPlayer(), packet.Password);
                    }
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChatLeaveChannel)]
        void HandleLeaveChannel(LeaveChannel packet)
        {
            if (string.IsNullOrEmpty(packet.ChannelName) && packet.ZoneChannelID == 0)
                return;

            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(GetPlayer().GetZoneId());
            if (packet.ZoneChannelID != 0)
            {
                ChatChannelsRecord channel = CliDB.ChatChannelsStorage.LookupByKey(packet.ZoneChannelID);
                if (channel == null)
                    return;

                if (zone == null || !GetPlayer().CanJoinConstantChannelInZone(channel, zone))
                    return;
            }

            ChannelManager cMgr = ChannelManager.ForTeam(GetPlayer().GetTeam());
            if (cMgr != null)
            {
                Channel channel = cMgr.GetChannel((uint)packet.ZoneChannelID, packet.ChannelName, GetPlayer(), true, zone);
                if (channel != null)
                    channel.LeaveChannel(GetPlayer(), true);

                if (packet.ZoneChannelID != 0)
                    cMgr.LeftChannel((uint)packet.ZoneChannelID, zone);
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChatChannelAnnouncements)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelDeclineInvite)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelDisplayList)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelList)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelOwner)]
        void HandleChannelCommand(ChannelCommand packet)
        {
            Channel channel = ChannelManager.GetChannelForPlayerByNamePart(packet.ChannelName, GetPlayer());
            if (channel == null)
                return;

            switch (packet.GetOpcode())
            {
                case ClientOpcodes.ChatChannelAnnouncements:
                    channel.Announce(GetPlayer());
                    break;
                case ClientOpcodes.ChatChannelDeclineInvite:
                    channel.DeclineInvite(GetPlayer());
                    break;
                case ClientOpcodes.ChatChannelDisplayList:
                case ClientOpcodes.ChatChannelList:
                    channel.List(GetPlayer());
                    break;
                case ClientOpcodes.ChatChannelOwner:
                    channel.SendWhoOwner(GetPlayer());
                    break;
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChatChannelBan)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelInvite)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelKick)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelModerator)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelSetOwner)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelSilenceAll)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelUnban)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelUnmoderator)]
        [WorldPacketHandler(ClientOpcodes.ChatChannelUnsilenceAll)]
        void HandleChannelPlayerCommand(ChannelPlayerCommand packet)
        {
            if (packet.Name.Length >= 49)
            {
                Log.outDebug(LogFilter.ChatSystem, "{0} {1} ChannelName: {2}, Name: {3}, Name too long.", packet.GetOpcode(), GetPlayerInfo(), packet.ChannelName, packet.Name);
                return;
            }            

            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            Channel channel = ChannelManager.GetChannelForPlayerByNamePart(packet.ChannelName, GetPlayer());
            if (channel == null)
                return;

            switch (packet.GetOpcode())
            {
                case ClientOpcodes.ChatChannelBan:
                    channel.Ban(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelInvite:
                    channel.Invite(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelKick:
                    channel.Kick(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelModerator:
                    channel.SetModerator(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelSetOwner:
                    channel.SetOwner(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelSilenceAll:
                    channel.SilenceAll(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelUnban:
                    channel.UnBan(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelUnmoderator:
                    channel.UnsetModerator(GetPlayer(), packet.Name);
                    break;
                case ClientOpcodes.ChatChannelUnsilenceAll:
                    channel.UnsilenceAll(GetPlayer(), packet.Name);
                    break;
            }
        }

        [WorldPacketHandler(ClientOpcodes.ChatChannelPassword)]
        void HandleChannelPassword(ChannelPassword packet)
        {
            if (packet.Password.Length > 31)
            {
                Log.outDebug(LogFilter.ChatSystem, "{0} {1} ChannelName: {2}, Password: {3}, Password too long.",
                packet.GetOpcode(), GetPlayerInfo(), packet.ChannelName, packet.Password);
                return;
            }

            Log.outDebug(LogFilter.ChatSystem, "{0} {1} ChannelName: {2}, Password: {3}", packet.GetOpcode(), GetPlayerInfo(), packet.ChannelName, packet.Password);

            Channel channel = ChannelManager.GetChannelForPlayerByNamePart(packet.ChannelName, GetPlayer());
            if (channel != null)
                channel.Password(GetPlayer(), packet.Password);
        }
    }
}
