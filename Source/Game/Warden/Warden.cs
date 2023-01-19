// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Cryptography;
using Framework.IO;
using Game.Networking.Packets;
using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Game
{
    public abstract class Warden
    {
        internal WorldSession _session;
        internal byte[] _inputKey = new byte[16];
        internal byte[] _outputKey = new byte[16];
        internal byte[] _seed = new byte[16];
        internal SARC4 _inputCrypto;
        internal SARC4 _outputCrypto;
        internal uint _checkTimer;                          // Timer for sending check requests
        internal uint _clientResponseTimer;                 // Timer for client response delay
        internal bool _dataSent;
        internal ClientWardenModule _module;
        internal bool _initialized;

        protected Warden()
        {
            _inputCrypto = new SARC4();
            _outputCrypto = new SARC4();
            _checkTimer = 10 * Time.InMilliseconds;
        }

        public void MakeModuleForClient()
        {
            Log.outDebug(LogFilter.Warden, "Make module for client");
            InitializeModuleForClient(out _module);

            // md5 hash
            MD5 ctx = MD5.Create();
            ctx.Initialize();
            ctx.TransformBlock(_module.CompressedData, 0, _module.CompressedData.Length, _module.CompressedData, 0);
            ctx.TransformBlock(_module.Id, 0, _module.Id.Length, _module.Id, 0);
        }

        public void SendModuleToClient()
        {
            Log.outDebug(LogFilter.Warden, "Send module to client");

            // Create packet structure
            WardenModuleTransfer packet = new();

            uint sizeLeft = _module.CompressedSize;
            int pos = 0;
            uint burstSize;
            while (sizeLeft > 0)
            {
                burstSize = sizeLeft < 500 ? sizeLeft : 500u;
                packet.Command = WardenOpcodes.SmsgModuleCache;
                packet.DataSize = (ushort)burstSize;
                Buffer.BlockCopy(_module.CompressedData, pos, packet.Data, 0, (int)burstSize);
                sizeLeft -= burstSize;
                pos += (int)burstSize;

                Warden3DataServer pkt1 = new();
                pkt1.Data = EncryptData(packet);
                _session.SendPacket(pkt1);
            }
        }

        public void RequestModule()
        {
            Log.outDebug(LogFilter.Warden, "Request module");

            // Create packet structure
            WardenModuleUse request = new();
            request.Command = WardenOpcodes.SmsgModuleUse;

            request.ModuleId = _module.Id;
            request.ModuleKey = _module.Key;
            request.Size = _module.CompressedSize;

            Warden3DataServer packet = new();
            packet.Data = EncryptData(request);
            _session.SendPacket(packet);
        }

        public void Update(uint diff)
        {
            if (!_initialized)
                return;

            if (_dataSent)
            {
                uint maxClientResponseDelay = WorldConfig.GetUIntValue(WorldCfg.WardenClientResponseDelay);
                if (maxClientResponseDelay > 0)
                {
                    // Kick player if client response delays more than set in config
                    if (_clientResponseTimer > maxClientResponseDelay * Time.InMilliseconds)
                    {
                        Log.outWarn(LogFilter.Warden, "{0} (latency: {1}, IP: {2}) exceeded Warden module response delay for more than {3} - disconnecting client",
                                       _session.GetPlayerInfo(), _session.GetLatency(), _session.GetRemoteAddress(), Time.secsToTimeString(maxClientResponseDelay, TimeFormat.ShortText));
                        _session.KickPlayer("Warden::Update Warden module response delay exceeded");
                    }
                    else
                        _clientResponseTimer += diff;
                }
            }
            else
            {
                if (diff >= _checkTimer)
                    RequestChecks();
                else
                    _checkTimer -= diff;
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
                checkSum ^= BitConverter.ToUInt32(hash, i * 4);

            return checkSum;
        }

        public string ApplyPenalty(WardenCheck check = null)
        {
            WardenActions action;

            if (check != null)
                action = check.Action;
            else
                action = (WardenActions)WorldConfig.GetIntValue(WorldCfg.WardenClientFailAction);

            switch (action)
            {
                case WardenActions.Kick:
                    _session.KickPlayer("Warden::Penalty");
                    break;
                case WardenActions.Ban:
                {
                    Global.AccountMgr.GetName(_session.GetAccountId(), out string accountName);
                    string banReason = "Warden Anticheat Violation";
                    // Check can be NULL, for example if the client sent a wrong signature in the warden packet (CHECKSUM FAIL)
                    if (check != null)
                        banReason += ": " + check.Comment + " (CheckId: " + check.CheckId + ")";

                    Global.WorldMgr.BanAccount(BanMode.Account, accountName, WorldConfig.GetUIntValue(WorldCfg.WardenClientBanDuration), banReason, "Server");
                    break;
                }
                case WardenActions.Log:
                default:
                    return "None";
            }

            return action.ToString();
        }

        public void HandleData(ByteBuffer buff)
        {
            byte[] data = buff.GetData();
            DecryptData(data);
            var opcode = data[0];
            Log.outDebug(LogFilter.Warden, $"Got packet, opcode 0x{opcode:X}, size {data.Length - 1}");

            switch ((WardenOpcodes)opcode)
            {
                case WardenOpcodes.CmsgModuleMissing:
                    SendModuleToClient();
                    break;
                case WardenOpcodes.CmsgModuleOk:
                    RequestHash();
                    break;
                case WardenOpcodes.CmsgCheatChecksResult:
                    HandleCheckResult(buff);
                    break;
                case WardenOpcodes.CmsgMemChecksResult:
                    Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MEM_CHECKS_RESULT received!");
                    break;
                case WardenOpcodes.CmsgHashResult:
                    HandleHashResult(buff);
                    InitializeModule();
                    break;
                case WardenOpcodes.CmsgModuleFailed:
                    Log.outDebug(LogFilter.Warden, "NYI WARDEN_CMSG_MODULE_FAILED received!");
                    break;
                default:
                    Log.outWarn(LogFilter.Warden, $"Got unknown warden opcode 0x{opcode:X} of size {data.Length - 1}.");
                    break;
            }
        }

        bool ProcessLuaCheckResponse(string msg)
        {
            string WARDEN_TOKEN = "_TW\t";
            if (!msg.StartsWith(WARDEN_TOKEN))
                return false;

            ushort id = 0;
            ushort.Parse(msg.Substring(WARDEN_TOKEN.Length - 1, 10));
            if (id < Global.WardenCheckMgr.GetMaxValidCheckId())
            {
                WardenCheck check = Global.WardenCheckMgr.GetCheckData(id);
                if (check.Type == WardenCheckType.LuaEval)
                {
                    string penalty1 = ApplyPenalty(check);
                    Log.outWarn(LogFilter.Warden, $"{_session.GetPlayerInfo()} failed Warden check {id} ({check.Type}). Action: {penalty1}");
                    return true;
                }
            }

            string penalty = ApplyPenalty(null);
            Log.outWarn(LogFilter.Warden, $"{_session.GetPlayerInfo()} sent bogus Lua check response for Warden. Action: {penalty}");
            return true;
        }

        public abstract void Init(WorldSession session, BigInteger k);

        public abstract void InitializeModule();

        public abstract void RequestHash();

        public abstract void HandleHashResult(ByteBuffer buff);

        public abstract void HandleCheckResult(ByteBuffer buff);

        public abstract void InitializeModuleForClient(out ClientWardenModule module);

        public abstract void RequestChecks();
    }

    class WardenModuleUse
    {
        public WardenOpcodes Command;
        public byte[] ModuleId = new byte[16];
        public byte[] ModuleKey = new byte[16];
        public uint Size;

        public static implicit operator byte[](WardenModuleUse use)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteUInt8((byte)use.Command);
            buffer.WriteBytes(use.ModuleId, 16);
            buffer.WriteBytes(use.ModuleKey, 16);
            buffer.WriteUInt32(use.Size);
            return buffer.GetData();
        }
    }

    class WardenModuleTransfer
    {
        public WardenOpcodes Command;
        public ushort DataSize;
        public byte[] Data = new byte[500];

        public static implicit operator byte[](WardenModuleTransfer transfer)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteUInt8((byte)transfer.Command);
            buffer.WriteUInt16(transfer.DataSize);
            buffer.WriteBytes(transfer.Data, 500);
            return buffer.GetData();
        }
    }

    class WardenHashRequest
    {
        public WardenOpcodes Command;
        public byte[] Seed = new byte[16];

        public static implicit operator byte[](WardenHashRequest request)
        {
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteUInt8((byte)request.Command);
            buffer.WriteBytes(request.Seed);
            return buffer.GetData();
        }
    }

    public class ClientWardenModule
    {
        public byte[] Id = new byte[16];
        public byte[] Key = new byte[16];
        public byte[] CompressedData;
        public uint CompressedSize;
    }
}
