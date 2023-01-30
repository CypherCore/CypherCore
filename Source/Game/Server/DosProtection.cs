// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Networking;

namespace Game
{
    public class DosProtection
    {
        private enum Policy
        {
            Log,
            Kick,
            Ban,
        }

        private readonly Policy _policy;
        private readonly WorldSession Session;
        private readonly Dictionary<uint, PacketCounter> _PacketThrottlingMap = new();

        public DosProtection(WorldSession s)
        {
            Session = s;
            _policy = (Policy)WorldConfig.GetIntValue(WorldCfg.PacketSpoofPolicy);
        }

        //todo fix me
        public bool EvaluateOpcode(WorldPacket packet, long time)
        {
            uint maxPacketCounterAllowed = 0;// GetMaxPacketCounterAllowed(p.GetOpcode());

            // Return true if there no limit for the opcode
            if (maxPacketCounterAllowed == 0)
                return true;

            if (!_PacketThrottlingMap.ContainsKey(packet.GetOpcode()))
                _PacketThrottlingMap[packet.GetOpcode()] = new PacketCounter();

            PacketCounter packetCounter = _PacketThrottlingMap[packet.GetOpcode()];
            if (packetCounter.LastReceiveTime != time)
            {
                packetCounter.LastReceiveTime = time;
                packetCounter.AmountCounter = 0;
            }

            // Check if player is flooding some packets
            if (++packetCounter.AmountCounter <= maxPacketCounterAllowed)
                return true;

            Log.outWarn(LogFilter.Network, "AntiDOS: Account {0}, IP: {1}, Ping: {2}, Character: {3}, flooding packet (opc: {4} (0x{4}), Count: {5})",
                Session.GetAccountId(), Session.GetRemoteAddress(), Session.GetLatency(), Session.GetPlayerName(), packet.GetOpcode(), packetCounter.AmountCounter);

            switch (_policy)
            {
                case Policy.Log:
                    return true;
                case Policy.Kick:
                    Log.outInfo(LogFilter.Network, "AntiDOS: Player kicked!");
                    return false;
                case Policy.Ban:
                    BanMode bm = (BanMode)WorldConfig.GetIntValue(WorldCfg.PacketSpoofBanmode);
                    uint duration = WorldConfig.GetUIntValue(WorldCfg.PacketSpoofBanduration); // in seconds
                    string nameOrIp = "";
                    switch (bm)
                    {
                        case BanMode.Character: // not supported, ban account
                        case BanMode.Account:
                            Global.AccountMgr.GetName(Session.GetAccountId(), out nameOrIp);
                            break;
                        case BanMode.IP:
                            nameOrIp = Session.GetRemoteAddress();
                            break;
                    }
                    Global.WorldMgr.BanAccount(bm, nameOrIp, duration, "DOS (Packet Flooding/Spoofing", "Server: AutoDOS");
                    Log.outInfo(LogFilter.Network, "AntiDOS: Player automatically banned for {0} seconds.", duration);
                    return false;
            }
            return true;
        }
    }
}
