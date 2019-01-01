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
using Framework.Cryptography;
using Framework.IO;
using Game.Network.Packets;
using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Game
{
    public abstract class Warden
    {
        protected Warden()
        {
            _inputCrypto = new SARC4();
            _outputCrypto = new SARC4();
            _checkTimer = 10 * Time.InMilliseconds;
        }

        public abstract void Init(WorldSession session, BigInteger k);
        public abstract ClientWardenModule GetModuleForClient();
        public abstract void InitializeModule();
        public abstract void RequestHash();
        public abstract void HandleHashResult(ByteBuffer buff);
        public abstract void RequestData();
        public abstract void HandleData(ByteBuffer buff);

        public void SendModuleToClient()
        {
            Log.outDebug(LogFilter.Warden, "Send module to client");

            uint sizeLeft = _module.CompressedSize;
            int pos = 0;
            uint burstSize;
            while (sizeLeft > 0)
            {
                WardenModuleTransfer transfer = new WardenModuleTransfer();

                burstSize = sizeLeft < 500 ? sizeLeft : 500u;
                transfer.Command = WardenOpcodes.Smsg_ModuleCache;
                transfer.DataSize = (ushort)burstSize;
                Buffer.BlockCopy(_module.CompressedData, pos, transfer.Data, 0, (int)burstSize);
                sizeLeft -= burstSize;
                pos += (int)burstSize;

                WardenDataServer packet = new WardenDataServer();
                packet.Data = EncryptData(transfer.Write());
                _session.SendPacket(packet);
            }
        }

        public void RequestModule()
        {
            Log.outDebug(LogFilter.Warden, "Request module");

            // Create packet structure
            WardenModuleUse request = new WardenModuleUse();
            request.Command = WardenOpcodes.Smsg_ModuleUse;

            request.ModuleId = _module.Id;
            request.ModuleKey = _module.Key;
            request.Size = _module.CompressedSize;

            WardenDataServer packet = new WardenDataServer();
            packet.Data = EncryptData(request.Write());
            _session.SendPacket(packet);
        }

        public void Update()
        {
            if (_initialized)
            {
                uint currentTimestamp = Time.GetMSTime();
                uint diff = currentTimestamp - _previousTimestamp;
                _previousTimestamp = currentTimestamp;

                if (_dataSent)
                {
                    uint maxClientResponseDelay = WorldConfig.GetUIntValue(WorldCfg.WardenClientResponseDelay);
                    if (maxClientResponseDelay > 0)
                    {
                        // Kick player if client response delays more than set in config
                        if (_clientResponseTimer > maxClientResponseDelay * Time.InMilliseconds)
                        {
                            Log.outWarn(LogFilter.Warden, "{0} (latency: {1}, IP: {2}) exceeded Warden module response delay for more than {3} - disconnecting client",
                                           _session.GetPlayerInfo(), _session.GetLatency(), _session.GetRemoteAddress(), Time.secsToTimeString(maxClientResponseDelay, true));
                            _session.KickPlayer();
                        }
                        else
                            _clientResponseTimer += diff;
                    }
                }
                else
                {
                    if (diff >= _checkTimer)
                    {
                        RequestData();
                    }
                    else
                        _checkTimer -= diff;
                }
            }
        }

        public void DecryptData(byte[] buffer)
        {
            _inputCrypto.ProcessBuffer(buffer, buffer.Length);
        }

        public ByteBuffer EncryptData(byte[] buffer)
        {
            _outputCrypto.ProcessBuffer(buffer, buffer.Length);
            return new ByteBuffer(buffer);
        }

        public bool IsValidCheckSum(uint checksum, byte[] data, ushort length)
        {
            uint newChecksum = BuildChecksum(data, length);

            if (checksum != newChecksum)
            {
                Log.outDebug(LogFilter.Warden, "CHECKSUM IS NOT VALID");
                return false;
            }
            else
            {
                Log.outDebug(LogFilter.Warden, "CHECKSUM IS VALID");
                return true;
            }
        }

        public uint BuildChecksum(byte[] data, uint length)
        {
            SHA1 sha = SHA1.Create();

            var hash = sha.ComputeHash(data, 0, (int)length);
            uint checkSum = 0;
            for (byte i = 0; i < 5; ++i)
                checkSum = checkSum ^ BitConverter.ToUInt32(hash, i * 4);

            return checkSum;
        }

        public string Penalty(WardenCheck check = null)
        {
            WardenActions action;

            if (check != null)
                action = check.Action;
            else
                action = (WardenActions)WorldConfig.GetIntValue(WorldCfg.WardenClientFailAction);

            switch (action)
            {
                case WardenActions.Log:
                    return "None";
                case WardenActions.Kick:
                    _session.KickPlayer();
                    return "Kick";
                case WardenActions.Ban:
                    {
                        string duration = WorldConfig.GetIntValue(WorldCfg.WardenClientBanDuration) + "s";
                        string accountName;
                        Global.AccountMgr.GetName(_session.GetAccountId(), out accountName);
                        string banReason = "Warden Anticheat Violation";
                        // Check can be NULL, for example if the client sent a wrong signature in the warden packet (CHECKSUM FAIL)
                        if (check != null)
                            banReason += ": " + check.Comment + " (CheckId: " + check.CheckId + ")";

                        Global.WorldMgr.BanAccount(BanMode.Account, accountName, duration, banReason, "Server");
                        return "Ban";
                    }
                default:
                    break;
            }
            return "Undefined";
        }

        internal WorldSession _session;
        internal byte[] _inputKey = new byte[16];
        internal byte[] _outputKey = new byte[16];
        internal byte[] _seed = new byte[16];
        internal SARC4 _inputCrypto;
        internal SARC4 _outputCrypto;
        internal uint _checkTimer;                          // Timer for sending check requests
        internal uint _clientResponseTimer;                 // Timer for client response delay
        internal bool _dataSent;
        internal uint _previousTimestamp;
        internal ClientWardenModule _module;
        internal bool _initialized;
    }

    public class ClientWardenModule
    {
        public byte[] Id = new byte[16];
        public byte[] Key = new byte[16];
        public uint CompressedSize;
        public byte[] CompressedData;
    }


}
