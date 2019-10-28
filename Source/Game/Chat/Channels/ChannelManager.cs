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
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    public class ChannelManager
    {
        public ChannelManager(Team team)
        {
            _team = team;
            _guidGenerator = new ObjectGuidGenerator(HighGuid.ChatChannel);
        }

        public static ChannelManager ForTeam(Team team)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionChannel))
                return allianceChannelMgr;        // cross-faction

            if (team == Team.Alliance)
                return allianceChannelMgr;

            if (team == Team.Horde)
                return hordeChannelMgr;

            return null;
        }

        public static Channel GetChannelForPlayerByNamePart(string namePart, Player playerSearcher)
        {
            foreach (Channel channel in playerSearcher.GetJoinedChannels())
            {
                string chanName = channel.GetName(playerSearcher.GetSession().GetSessionDbcLocale());
                if (chanName.ToLower().Equals(namePart.ToLower()))
                    return channel;
            }

            return null;
        }

        public Channel GetJoinChannel(uint channelId, string name, AreaTableRecord zoneEntry = null)
        {
            if (channelId != 0) // builtin
            {
                ObjectGuid channelGuid = CreateBuiltinChannelGuid(channelId, zoneEntry);
                var channel = _channels.LookupByKey(channelGuid);
                if (channel != null)
                    return channel;

                Channel newChannel = new Channel(channelGuid, channelId, _team, zoneEntry);
                _channels[channelGuid] = newChannel;
                return newChannel;
            }
            else // custom
            {
                var channel = _customChannels.LookupByKey(name.ToLower());
                if (channel != null)
                    return channel;

                Channel newChannel = new Channel(CreateCustomChannelGuid(), name, _team);
                _customChannels[name.ToLower()] = newChannel;
                return newChannel;
            }
        }

        public Channel GetChannel(uint channelId, string name, Player player, bool notify = true, AreaTableRecord zoneEntry = null)
        {
            Channel result = null;
            if (channelId != 0) // builtin
            {
                var channel = _channels.LookupByKey(CreateBuiltinChannelGuid(channelId, zoneEntry));
                if (channel != null)
                    result = channel;
            }
            else // custom
            {
                var channel = _customChannels.LookupByKey(name.ToLower());
                if (channel != null)
                    result = channel;
            }

            if (result == null && notify)
            {
                string channelName = name;
                Channel.GetChannelName(ref channelName, channelId, player.GetSession().GetSessionDbcLocale(), zoneEntry);

                SendNotOnChannelNotify(player, channelName);
            }

            return result;
        }

        public void LeftChannel(string name)
        {
            string channelName = name.ToLower();

            var channel = _customChannels.LookupByKey(channelName);
            if (channel == null)
                return;

            if (channel.GetNumPlayers() == 0)
                _customChannels.Remove(channelName);
        }

        public void LeftChannel(uint channelId, AreaTableRecord zoneEntry)
        {
            var guid = CreateBuiltinChannelGuid(channelId, zoneEntry);
            var channel = _channels.LookupByKey(guid);
            if (channel == null)
                return;

            if (channel.GetNumPlayers() == 0)
                _channels.Remove(guid);
        }

        public static void SendNotOnChannelNotify(Player player, string name)
        {
            ChannelNotify notify = new ChannelNotify();
            notify.Type = ChatNotify.NotMemberNotice;
            notify.Channel = name;
            player.SendPacket(notify);
        }

        ObjectGuid CreateCustomChannelGuid()
        {
            ulong high = 0;
            high |= (ulong)HighGuid.ChatChannel << 58;
            high |= (ulong)Global.WorldMgr.GetRealmId().Realm << 42;
            high |= (ulong)(_team == Team.Alliance ? 3 : 5) << 4;

            ObjectGuid channelGuid = new ObjectGuid();
            channelGuid.SetRawValue(high, _guidGenerator.Generate());
            return channelGuid;
        }

        ObjectGuid CreateBuiltinChannelGuid(uint channelId, AreaTableRecord zoneEntry = null)
        {

            ChatChannelsRecord channelEntry = CliDB.ChatChannelsStorage.LookupByKey(channelId);
            uint zoneId = zoneEntry != null ? zoneEntry.Id : 0;
            if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global | ChannelDBCFlags.CityOnly))
                zoneId = 0;

            ulong high = 0;
            high |= (ulong)HighGuid.ChatChannel << 58;
            high |= (ulong)Global.WorldMgr.GetRealmId().Realm << 42;
            high |= 1ul << 25; // built-in
            if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly2))
                high |= 1ul << 24; // trade

            high |= (ulong)(zoneId) << 10;
            high |= (ulong)(_team == Team.Alliance ? 3 : 5) << 4;

            ObjectGuid channelGuid = new ObjectGuid();
            channelGuid.SetRawValue(high, channelId);
            return channelGuid;
        }

        Dictionary<string, Channel> _customChannels = new Dictionary<string, Channel>();
        Dictionary<ObjectGuid, Channel> _channels = new Dictionary<ObjectGuid, Channel>();
        Team _team;
        ObjectGuidGenerator _guidGenerator;

        static ChannelManager allianceChannelMgr = new ChannelManager(Team.Alliance);
        static ChannelManager hordeChannelMgr = new ChannelManager(Team.Horde);
    }
}
