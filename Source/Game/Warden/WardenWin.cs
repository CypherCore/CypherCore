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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game
{
    class WardenWin : Warden
    {
        public override void Init(WorldSession session, BigInteger k)
        {
            _session = session;
            // Generate Warden Key
            SHA1Randx WK = new SHA1Randx(k.ToByteArray());
            WK.Generate(_inputKey, 16);
            WK.Generate(_outputKey, 16);

            _seed = WardenModuleWin.Seed;

            _inputCrypto.PrepareKey(_inputKey);
            _outputCrypto.PrepareKey(_outputKey);
            Log.outDebug(LogFilter.Warden, "Server side warden for client {0} initializing...", session.GetAccountId());
            Log.outDebug(LogFilter.Warden, "C->S Key: {0}", _inputKey.ToHexString());
            Log.outDebug(LogFilter.Warden, "S->C Key: {0}", _outputKey.ToHexString());
            Log.outDebug(LogFilter.Warden, "  Seed: {0}", _seed.ToHexString());
            Log.outDebug(LogFilter.Warden, "Loading Module...");

            _module = GetModuleForClient();

            Log.outDebug(LogFilter.Warden, "Module Key: {0}", _module.Key.ToHexString());
            Log.outDebug(LogFilter.Warden, "Module ID: {0}", _module.Id.ToHexString());
            RequestModule();
        }

        public override ClientWardenModule GetModuleForClient()
        {
            ClientWardenModule mod = new ClientWardenModule();

            uint length = (uint)WardenModuleWin.Module.Length;

            // data assign
            mod.CompressedSize = length;
            mod.CompressedData = WardenModuleWin.Module;
            mod.Key = WardenModuleWin.ModuleKey;

            // md5 hash
            System.Security.Cryptography.MD5 ctx = System.Security.Cryptography.MD5.Create();
            ctx.Initialize();
            ctx.TransformBlock(mod.CompressedData, 0, mod.CompressedData.Length, mod.CompressedData, 0);
            ctx.TransformBlock(mod.Id, 0, mod.Id.Length, mod.Id, 0);

            return mod;
        }

        public override void InitializeModule()
        {
            Log.outDebug(LogFilter.Warden, "Initialize module");

            // Create packet structure
            WardenInitModuleRequest Request = new WardenInitModuleRequest();
            Request.Command1 = WardenOpcodes.Smsg_ModuleInitialize;
            Request.Size1 = 20;
            Request.Unk1 = 1;
            Request.Unk2 = 0;
            Request.Type = 1;
            Request.String_library1 = 0;
            Request.Function1[0] = 0x00024F80;                      // 0x00400000 + 0x00024F80 SFileOpenFile
            Request.Function1[1] = 0x000218C0;                      // 0x00400000 + 0x000218C0 SFileGetFileSize
            Request.Function1[2] = 0x00022530;                      // 0x00400000 + 0x00022530 SFileReadFile
            Request.Function1[3] = 0x00022910;                      // 0x00400000 + 0x00022910 SFileCloseFile
            Request.CheckSumm1 = BuildChecksum(BitConverter.GetBytes(Request.Unk1), 20);

            Request.Command2 = WardenOpcodes.Smsg_ModuleInitialize;
            Request.Size2 = 8;
            Request.Unk3 = 4;
            Request.Unk4 = 0;
            Request.String_library2 = 0;
            Request.Function2 = 0x00419D40;                         // 0x00400000 + 0x00419D40 FrameScript::GetText
            Request.Function2_set = 1;
            Request.CheckSumm2 = BuildChecksum(BitConverter.GetBytes(Request.Unk2), 8);

            Request.Command3 = WardenOpcodes.Smsg_ModuleInitialize;
            Request.Size3 = 8;
            Request.Unk5 = 1;
            Request.Unk6 = 1;
            Request.String_library3 = 0;
            Request.Function3 = 0x0046AE20;                         // 0x00400000 + 0x0046AE20 PerformanceCounter
            Request.Function3_set = 1;
            Request.CheckSumm3 = BuildChecksum(BitConverter.GetBytes(Request.Unk5), 8);

            WardenDataServer packet = new WardenDataServer();
            packet.Data = EncryptData(Request.Write());
            _session.SendPacket(packet);
        }

        public override void RequestHash()
        {
            Log.outDebug(LogFilter.Warden, "Request hash");

            // Create packet structure
            WardenHashRequest Request = new WardenHashRequest();
            Request.Command = WardenOpcodes.Smsg_HashRequest;
            Request.Seed = _seed;

            WardenDataServer packet = new WardenDataServer();
            packet.Data = EncryptData(Request.Write());
            _session.SendPacket(packet);
        }

        public override void HandleHashResult(ByteBuffer buff)
        {
            // Verify key
            if (buff.ReadBytes(20) != WardenModuleWin.ClientKeySeedHash)
            {
                Log.outWarn(LogFilter.Warden, "{0} failed hash reply. Action: {0}", _session.GetPlayerInfo(), Penalty());
                return;
            }

            Log.outDebug(LogFilter.Warden, "Request hash reply: succeed");

            // Change keys here
            _inputKey = WardenModuleWin.ClientKeySeed;
            _outputKey = WardenModuleWin.ServerKeySeed;

            _inputCrypto.PrepareKey(_inputKey);
            _outputCrypto.PrepareKey(_outputKey);

            _initialized = true;

            _previousTimestamp = Time.GetMSTime();
        }

        public override void RequestData()
        {
            Log.outDebug(LogFilter.Warden, "Request data");

            // If all checks were done, fill the todo list again
            if (_memChecksTodo.Empty())
                _memChecksTodo.AddRange(Global.WardenCheckMgr.MemChecksIdPool);

            if (_otherChecksTodo.Empty())
                _otherChecksTodo.AddRange(Global.WardenCheckMgr.OtherChecksIdPool);

            _serverTicks = Time.GetMSTime();

            ushort id;
            WardenCheckType type;
            WardenCheck wd;
            _currentChecks.Clear();

            // Build check request
            for (uint i = 0; i < WorldConfig.GetUIntValue(WorldCfg.WardenNumMemChecks); ++i)
            {
                // If todo list is done break loop (will be filled on next Update() run)
                if (_memChecksTodo.Empty())
                    break;

                // Get check id from the end and remove it from todo
                id = _memChecksTodo.Last();
                _memChecksTodo.Remove(id);

                // Add the id to the list sent in this cycle
                _currentChecks.Add(id);
            }

            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteUInt8(WardenOpcodes.Smsg_CheatChecksRequest);

            for (uint i = 0; i < WorldConfig.GetUIntValue(WorldCfg.WardenNumOtherChecks); ++i)
            {
                // If todo list is done break loop (will be filled on next Update() run)
                if (_otherChecksTodo.Empty())
                    break;

                // Get check id from the end and remove it from todo
                id = _otherChecksTodo.Last();
                _otherChecksTodo.Remove(id);

                // Add the id to the list sent in this cycle
                _currentChecks.Add(id);

                wd = Global.WardenCheckMgr.GetWardenDataById(id);

                switch (wd.Type)
                {
                    case WardenCheckType.MPQ:
                    case WardenCheckType.LuaStr:
                    case WardenCheckType.Driver:
                        buffer.WriteUInt8(wd.Str.GetByteCount());
                        buffer.WriteString(wd.Str);
                        break;
                    default:
                        break;
                }
            }

            byte xorByte = _inputKey[0];

            // Add TIMING_CHECK
            buffer.WriteUInt8(0x00);
            buffer.WriteUInt8((int)WardenCheckType.Timing ^ xorByte);

            byte index = 1;

            foreach (var checkId in _currentChecks)
            {
                wd = Global.WardenCheckMgr.GetWardenDataById(checkId);

                type = wd.Type;
                buffer.WriteUInt8((int)type ^ xorByte);
                switch (type)
                {
                    case WardenCheckType.Memory:
                        {
                            buffer.WriteUInt8(0x00);
                            buffer.WriteUInt32(wd.Address);
                            buffer.WriteUInt8(wd.Length);
                            break;
                        }
                    case WardenCheckType.PageA:
                    case WardenCheckType.PageB:
                        {
                            buffer.WriteBytes(wd.Data.ToByteArray());
                            buffer.WriteUInt32(wd.Address);
                            buffer.WriteUInt8(wd.Length);
                            break;
                        }
                    case WardenCheckType.MPQ:
                    case WardenCheckType.LuaStr:
                        {
                            buffer.WriteUInt8(index++);
                            break;
                        }
                    case WardenCheckType.Driver:
                        {
                            buffer.WriteBytes(wd.Data.ToByteArray());
                            buffer.WriteUInt8(index++);
                            break;
                        }
                    case WardenCheckType.Module:
                        {
                            uint seed = RandomHelper.Rand32();
                            buffer.WriteUInt32(seed);
                            HmacHash hmac = new HmacHash(BitConverter.GetBytes(seed));
                            hmac.Finish(wd.Str);
                            buffer.WriteBytes(hmac.Digest);
                            break;
                        }
                    /*case PROC_CHECK:
                    {
                        buff.append(wd.i.AsByteArray(0, false).get(), wd.i.GetNumBytes());
                        buff << uint8(index++);
                        buff << uint8(index++);
                        buff << uint32(wd.Address);
                        buff << uint8(wd.Length);
                        break;
                    }*/
                    default:
                        break;                                      // Should never happen
                }
            }
            buffer.WriteUInt8(xorByte);

            WardenDataServer packet = new WardenDataServer();
            packet.Data = EncryptData(buffer.GetData());
            _session.SendPacket(packet);

            _dataSent = true;

            string stream = "Sent check id's: ";
            foreach (var checkId in _currentChecks)
                stream += checkId + " ";

            Log.outDebug(LogFilter.Warden, stream);
        }

        public override void HandleData(ByteBuffer buff)
        {
            Log.outDebug(LogFilter.Warden, "Handle data");

            _dataSent = false;
            _clientResponseTimer = 0;

            ushort Length = buff.ReadUInt16();
            uint Checksum = buff.ReadUInt32();

            if (!IsValidCheckSum(Checksum, buff.GetData(), Length))
            {
                Log.outWarn(LogFilter.Warden, "{0} failed checksum. Action: {1}", _session.GetPlayerInfo(), Penalty());
                return;
            }

            // TIMING_CHECK
            {
                byte result = buff.ReadUInt8();
                // @todo test it.
                if (result == 0x00)
                {
                    Log.outWarn(LogFilter.Warden, "{0} failed timing check. Action: {1}", _session.GetPlayerInfo(), Penalty());
                    return;
                }

                uint newClientTicks = buff.ReadUInt32();

                uint ticksNow = Time.GetMSTime();
                uint ourTicks = newClientTicks + (ticksNow - _serverTicks);

                Log.outDebug(LogFilter.Warden, "ServerTicks {0}", ticksNow);         // Now
                Log.outDebug(LogFilter.Warden, "RequestTicks {0}", _serverTicks);    // At request
                Log.outDebug(LogFilter.Warden, "Ticks {0}", newClientTicks);         // At response
                Log.outDebug(LogFilter.Warden, "Ticks diff {0}", ourTicks - newClientTicks);
            }

            BigInteger rs;
            WardenCheck rd;
            WardenCheckType type;
            ushort checkFailed = 0;

            foreach (var id in _currentChecks)
            {
                rd = Global.WardenCheckMgr.GetWardenDataById(id);
                rs = Global.WardenCheckMgr.GetWardenResultById(id);

                type = rd.Type;
                switch (type)
                {
                    case WardenCheckType.Memory:
                        {
                            byte Mem_Result = buff.ReadUInt8();

                            if (Mem_Result != 0)
                            {
                                Log.outDebug(LogFilter.Warden, "RESULT MEM_CHECK not 0x00, CheckId {0} account Id {1}", id, _session.GetAccountId());
                                checkFailed = id;
                                continue;
                            }

                            if (buff.ReadBytes(rd.Length).Compare(rs.ToByteArray()))
                            {
                                Log.outDebug(LogFilter.Warden, "RESULT MEM_CHECK fail CheckId {0} account Id {1}", id, _session.GetAccountId());
                                checkFailed = id;
                                continue;
                            }

                            Log.outDebug(LogFilter.Warden, "RESULT MEM_CHECK passed CheckId {0} account Id {1}", id, _session.GetAccountId());
                            break;
                        }
                    case WardenCheckType.PageA:
                    case WardenCheckType.PageB:
                    case WardenCheckType.Driver:
                    case WardenCheckType.Module:
                        {
                            byte value = 0xE9;
                            if (buff.ReadUInt8() != value)
                            {
                                if (type == WardenCheckType.PageA || type == WardenCheckType.PageB)
                                    Log.outDebug(LogFilter.Warden, "RESULT PAGE_CHECK fail, CheckId {0} account Id {1}", id, _session.GetAccountId());
                                if (type == WardenCheckType.Module)
                                    Log.outDebug(LogFilter.Warden, "RESULT MODULE_CHECK fail, CheckId {0} account Id {1}", id, _session.GetAccountId());
                                if (type == WardenCheckType.Driver)
                                    Log.outDebug(LogFilter.Warden, "RESULT DRIVER_CHECK fail, CheckId {0} account Id {1}", id, _session.GetAccountId());
                                checkFailed = id;
                                continue;
                            }

                            if (type == WardenCheckType.PageA || type == WardenCheckType.PageB)
                                Log.outDebug(LogFilter.Warden, "RESULT PAGE_CHECK passed CheckId {0} account Id {1}", id, _session.GetAccountId());
                            else if (type == WardenCheckType.Module)
                                Log.outDebug(LogFilter.Warden, "RESULT MODULE_CHECK passed CheckId {0} account Id {1}", id, _session.GetAccountId());
                            else if (type == WardenCheckType.Driver)
                                Log.outDebug(LogFilter.Warden, "RESULT DRIVER_CHECK passed CheckId {0} account Id {1}", id, _session.GetAccountId());
                            break;
                        }
                    case WardenCheckType.LuaStr:
                        {
                            byte Lua_Result = buff.ReadUInt8();

                            if (Lua_Result != 0)
                            {
                                Log.outDebug(LogFilter.Warden, "RESULT LUA_STR_CHECK fail, CheckId {0} account Id {1}", id, _session.GetAccountId());
                                checkFailed = id;
                                continue;
                            }

                            byte luaStrLen = buff.ReadUInt8();
                            if (luaStrLen != 0)
                                Log.outDebug(LogFilter.Warden, "Lua string: {0}", buff.ReadString(luaStrLen));

                            Log.outDebug(LogFilter.Warden, "RESULT LUA_STR_CHECK passed, CheckId {0} account Id {1}", id, _session.GetAccountId());
                            break;
                        }
                    case WardenCheckType.MPQ:
                        {
                            byte Mpq_Result = buff.ReadUInt8();

                            if (Mpq_Result != 0)
                            {
                                Log.outDebug(LogFilter.Warden, "RESULT MPQ_CHECK not 0x00 account id {0}", _session.GetAccountId());
                                checkFailed = id;
                                continue;
                            }

                            if (!buff.ReadBytes(20).Compare(rs.ToByteArray())) // SHA1
                            {
                                Log.outDebug(LogFilter.Warden, "RESULT MPQ_CHECK fail, CheckId {0} account Id {1}", id, _session.GetAccountId());
                                checkFailed = id;
                                continue;
                            }

                            Log.outDebug(LogFilter.Warden, "RESULT MPQ_CHECK passed, CheckId {0} account Id {1}", id, _session.GetAccountId());
                            break;
                        }
                    default:                                        // Should never happen
                        break;
                }
            }

            if (checkFailed > 0)
            {
                WardenCheck check = Global.WardenCheckMgr.GetWardenDataById(checkFailed);
                Log.outWarn(LogFilter.Warden, "{0} failed Warden check {1}. Action: {2}", _session.GetPlayerInfo(), checkFailed, Penalty(check));
            }

            // Set hold off timer, minimum timer should at least be 1 second
            uint holdOff = WorldConfig.GetUIntValue(WorldCfg.WardenClientCheckHoldoff);
            _checkTimer = (holdOff < 1 ? 1 : holdOff) * Time.InMilliseconds;
        }

        uint _serverTicks;
        List<ushort> _otherChecksTodo = new List<ushort>();
        List<ushort> _memChecksTodo = new List<ushort>();
        List<ushort> _currentChecks = new List<ushort>();
    }
}
