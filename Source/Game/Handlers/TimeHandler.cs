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
using Game.Network;
using Game.Network.Packets;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.UiTimeRequest, Status = SessionStatus.Authed, Processing = PacketProcessing.Inplace)]
        void HandleUITimeRequest(UITimeRequest packet)
        {
            UITime response = new UITime();
            response.Time = (uint)Time.UnixTime;
            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.TimeSyncResponse, Processing = PacketProcessing.Inplace)]
        void HandleTimeSyncResponse(TimeSyncResponse packet)
        {
            Log.outDebug(LogFilter.Network, "CMSG_TIME_SYNC_RESP");

            if (packet.SequenceIndex != _player.m_timeSyncQueue.FirstOrDefault())
                Log.outError(LogFilter.Network, "Wrong time sync counter from player {0} (cheater?)", _player.GetName());

            Log.outDebug(LogFilter.Network, "Time sync received: counter {0}, client ticks {1}, time since last sync {2}", packet.SequenceIndex, packet.ClientTime, packet.ClientTime - _player.m_timeSyncClient);

            uint ourTicks = packet.ClientTime + (Time.GetMSTime() - _player.m_timeSyncServer);

            // diff should be small
            Log.outDebug(LogFilter.Network, "Our ticks: {0}, diff {1}, latency {2}", ourTicks, ourTicks - packet.ClientTime, GetLatency());

            _player.m_timeSyncClient = packet.ClientTime;
            _player.m_timeSyncQueue.Pop();
        }
    }
}
