// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Cryptography;
using Framework.IO;
using Game.Networking.Packets;

namespace Game
{
	internal class WardenWin : Warden
	{
		// GUILD is the shortest string that has no client validation (RAID only sends if in a raid group)
		private static string _luaEvalPrefix = "local S,T,R=SendAddonMessage,function()";
		private static string _luaEvalMidfix = " end R=S and T()if R then S('_TW',";
		private static string _luaEvalPostfix = ",'GUILD')end";
		private CategoryCheck[] _checks = new CategoryCheck[(int)WardenCheckCategory.Max];
		private List<ushort> _currentChecks = new();

		private uint _serverTicks;

		public WardenWin()
		{
			foreach (WardenCheckCategory category in Enum.GetValues<WardenCheckCategory>())
				_checks[(int)category] = new CategoryCheck(Global.WardenCheckMgr.GetAvailableChecks(category).Shuffle().ToList());
		}

		public override void Init(WorldSession session, BigInteger k)
		{
			_session = session;
			// Generate Warden Key
			SessionKeyGenerator WK = new(k.ToByteArray());
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

			MakeModuleForClient();

			Log.outDebug(LogFilter.Warden, "Module Key: {0}", _module.Key.ToHexString());
			Log.outDebug(LogFilter.Warden, "Module ID: {0}", _module.Id.ToHexString());
			RequestModule();
		}

		public override void InitializeModuleForClient(out ClientWardenModule module)
		{
			// _data assign
			module                = new ClientWardenModule();
			module.CompressedData = WardenModuleWin.Module;
			module.CompressedSize = (uint)WardenModuleWin.Module.Length;
			module.Key            = WardenModuleWin.ModuleKey;
		}

		public override void InitializeModule()
		{
			Log.outDebug(LogFilter.Warden, "Initialize module");

			// Create packet structure
			WardenInitModuleRequest Request = new();
			Request.Command1        = WardenOpcodes.SmsgModuleInitialize;
			Request.Size1           = 20;
			Request.Unk1            = 1;
			Request.Unk2            = 0;
			Request.Type            = 1;
			Request.String_library1 = 0;
			Request.Function1[0]    = 0x00024F80; // 0x00400000 + 0x00024F80 SFileOpenFile
			Request.Function1[1]    = 0x000218C0; // 0x00400000 + 0x000218C0 SFileGetFileSize
			Request.Function1[2]    = 0x00022530; // 0x00400000 + 0x00022530 SFileReadFile
			Request.Function1[3]    = 0x00022910; // 0x00400000 + 0x00022910 SFileCloseFile

			Request.CheckSumm1 = BuildChecksum(new byte[]
			                                   {
				                                   Request.Unk1
			                                   },
			                                   20);

			Request.Command2        = WardenOpcodes.SmsgModuleInitialize;
			Request.Size2           = 8;
			Request.Unk3            = 4;
			Request.Unk4            = 0;
			Request.String_library2 = 0;
			Request.Function2       = 0x00419D40; // 0x00400000 + 0x00419D40 FrameScript::GetText
			Request.Function2_set   = 1;

			Request.CheckSumm2 = BuildChecksum(new byte[]
			                                   {
				                                   Request.Unk2
			                                   },
			                                   8);

			Request.Command3        = WardenOpcodes.SmsgModuleInitialize;
			Request.Size3           = 8;
			Request.Unk5            = 1;
			Request.Unk6            = 1;
			Request.String_library3 = 0;
			Request.Function3       = 0x0046AE20; // 0x00400000 + 0x0046AE20 PerformanceCounter
			Request.Function3_set   = 1;

			Request.CheckSumm3 = BuildChecksum(new byte[]
			                                   {
				                                   Request.Unk5
			                                   },
			                                   8);

			Warden3DataServer packet = new();
			packet.Data = EncryptData(Request);
			_session.SendPacket(packet);
		}

		public override void RequestHash()
		{
			Log.outDebug(LogFilter.Warden, "Request hash");

			// Create packet structure
			WardenHashRequest Request = new();
			Request.Command = WardenOpcodes.SmsgHashRequest;
			Request.Seed    = _seed;

			Warden3DataServer packet = new();
			packet.Data = EncryptData(Request);
			_session.SendPacket(packet);
		}

		public override void HandleHashResult(ByteBuffer buff)
		{
			// Verify key
			if (buff.ReadBytes(20) != WardenModuleWin.ClientKeySeedHash)
			{
				string penalty = ApplyPenalty();
				Log.outWarn(LogFilter.Warden, "{0} failed hash reply. Action: {0}", _session.GetPlayerInfo(), penalty);

				return;
			}

			Log.outDebug(LogFilter.Warden, "Request hash reply: succeed");

			// Change keys here
			_inputKey  = WardenModuleWin.ClientKeySeed;
			_outputKey = WardenModuleWin.ServerKeySeed;

			_inputCrypto.PrepareKey(_inputKey);
			_outputCrypto.PrepareKey(_outputKey);

			_initialized = true;
		}

		private static byte GetCheckPacketBaseSize(WardenCheckType type)
		{
			return type switch
			       {
				       WardenCheckType.Driver  => 1,
				       WardenCheckType.LuaEval => (byte)(1 + _luaEvalPrefix.Length - 1 + _luaEvalMidfix.Length - 1 + 4 + _luaEvalPostfix.Length - 1),
				       WardenCheckType.Mpq     => 1,
				       WardenCheckType.PageA   => 4 + 1,
				       WardenCheckType.PageB   => 4 + 1,
				       WardenCheckType.Module  => 4 + 20,
				       WardenCheckType.Mem     => 1 + 4 + 1,
				       _                       => 0
			       };
		}

		private static ushort GetCheckPacketSize(WardenCheck check)
		{
			int size = 1 + GetCheckPacketBaseSize(check.Type); // 1 byte check Type

			if (!check.Str.IsEmpty())
				size += (check.Str.Length + 1); // 1 byte string length

			if (!check.Data.Empty())
				size += check.Data.Length;

			return (ushort)size;
		}

		public override void RequestChecks()
		{
			Log.outDebug(LogFilter.Warden, $"Request _data from {_session.GetPlayerName()} (account {_session.GetAccountId()}) - loaded: {_session.GetPlayer() && !_session.PlayerLoading()}");

			// If all checks for a category are done, fill its todo list again
			foreach (WardenCheckCategory category in Enum.GetValues<WardenCheckCategory>())
			{
				var checks = _checks[(int)category];

				if (checks.IsAtEnd() &&
				    !checks.Empty())
				{
					Log.outDebug(LogFilter.Warden, $"Finished all {category} checks, re-shuffling");
					checks.Shuffle();
				}
			}

			_serverTicks = GameTime.GetGameTimeMS();
			_currentChecks.Clear();

			// Build check request
			ByteBuffer buff = new();
			buff.WriteUInt8((byte)WardenOpcodes.SmsgCheatChecksRequest);

			foreach (var category in Enum.GetValues<WardenCheckCategory>())
			{
				if (WardenCheckManager.IsWardenCategoryInWorldOnly(category) &&
				    !_session.GetPlayer())
					continue;

				var checks = _checks[(int)category];

				for (uint i = 0, n = WorldConfig.GetUIntValue(WardenCheckManager.GetWardenCategoryCountConfig(category)); i < n; ++i)
				{
					if (checks.IsAtEnd()) // all checks were already sent, list will be re-filled on next Update() run
						break;

					_currentChecks.Add(checks.currentIndex++);
				}
			}

			_currentChecks = _currentChecks.Shuffle().ToList();

			ushort expectedSize = 4;

			_currentChecks.RemoveAll(id =>
			                         {
				                         ushort thisSize = GetCheckPacketSize(Global.WardenCheckMgr.GetCheckData(id));

				                         if ((expectedSize + thisSize) > 450) // warden packets are truncated to 512 bytes clientside
					                         return true;

				                         expectedSize += thisSize;

				                         return false;
			                         });

			foreach (var id in _currentChecks)
			{
				WardenCheck check = Global.WardenCheckMgr.GetCheckData(id);

				if (check.Type == WardenCheckType.LuaEval)
				{
					buff.WriteUInt8((byte)(_luaEvalPrefix.Length - 1 + check.Str.Length + _luaEvalMidfix.Length - 1 + check.IdStr.Length + _luaEvalPostfix.Length - 1));
					buff.WriteString(_luaEvalPrefix);
					buff.WriteString(check.Str);
					buff.WriteString(_luaEvalMidfix);
					buff.WriteString(check.IdStr.ToString());
					buff.WriteString(_luaEvalPostfix);
				}
				else if (!check.Str.IsEmpty())
				{
					buff.WriteUInt8((byte)check.Str.GetByteCount());
					buff.WriteString(check.Str);
				}
			}

			byte xorByte = _inputKey[0];

			// Add TIMING_CHECK
			buff.WriteUInt8(0x00);
			buff.WriteUInt8((byte)((int)WardenCheckType.Timing ^ xorByte));

			byte index = 1;

			foreach (var checkId in _currentChecks)
			{
				WardenCheck check = Global.WardenCheckMgr.GetCheckData(checkId);

				var type = check.Type;
				buff.WriteUInt8((byte)((int)type ^ xorByte));

				switch (type)
				{
					case WardenCheckType.Mem:
					{
						buff.WriteUInt8(0x00);
						buff.WriteUInt32(check.Address);
						buff.WriteUInt8(check.Length);

						break;
					}
					case WardenCheckType.PageA:
					case WardenCheckType.PageB:
					{
						buff.WriteBytes(check.Data);
						buff.WriteUInt32(check.Address);
						buff.WriteUInt8(check.Length);

						break;
					}
					case WardenCheckType.Mpq:
					case WardenCheckType.LuaEval:
					{
						buff.WriteUInt8(index++);

						break;
					}
					case WardenCheckType.Driver:
					{
						buff.WriteBytes(check.Data);
						buff.WriteUInt8(index++);

						break;
					}
					case WardenCheckType.Module:
					{
						uint seed = RandomHelper.Rand32();
						buff.WriteUInt32(seed);
						HmacHash hmac = new(BitConverter.GetBytes(seed));
						hmac.Finish(check.Str);
						buff.WriteBytes(hmac.Digest);

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
						break; // Should never happen
				}
			}

			buff.WriteUInt8(xorByte);

			string idstring = "";

			foreach (var id in _currentChecks)
				idstring += $"{id} ";

			if (buff.GetSize() == expectedSize)
			{
				Log.outDebug(LogFilter.Warden, $"Finished building warden packet, size is {buff.GetSize()} bytes");
				Log.outDebug(LogFilter.Warden, $"Sent checks: {idstring}");
			}
			else
			{
				Log.outWarn(LogFilter.Warden, $"Finished building warden packet, size is {buff.GetSize()} bytes, but expected {expectedSize} bytes!");
				Log.outWarn(LogFilter.Warden, $"Sent checks: {idstring}");
			}

			Warden3DataServer packet = new();
			packet.Data = EncryptData(buff.GetData());
			_session.SendPacket(packet);

			_dataSent = true;
		}

		public override void HandleCheckResult(ByteBuffer buff)
		{
			Log.outDebug(LogFilter.Warden, "Handle _data");

			_dataSent            = false;
			_clientResponseTimer = 0;

			ushort Length   = buff.ReadUInt16();
			uint   Checksum = buff.ReadUInt32();

			if (!IsValidCheckSum(Checksum, buff.GetData(), Length))
			{
				string penalty = ApplyPenalty();
				Log.outWarn(LogFilter.Warden, "{0} failed checksum. Action: {1}", _session.GetPlayerInfo(), penalty);

				return;
			}

			// TIMING_CHECK
			{
				byte result = buff.ReadUInt8();

				// @todo test it.
				if (result == 0x00)
				{
					string penalty = ApplyPenalty();
					Log.outWarn(LogFilter.Warden, "{0} failed timing check. Action: {1}", _session.GetPlayerInfo(), penalty);

					return;
				}

				uint newClientTicks = buff.ReadUInt32();

				uint ticksNow = GameTime.GetGameTimeMS();
				uint ourTicks = newClientTicks + (ticksNow - _serverTicks);

				Log.outDebug(LogFilter.Warden, "ServerTicks {0}", ticksNow);      // Now
				Log.outDebug(LogFilter.Warden, "RequestTicks {0}", _serverTicks); // At request
				Log.outDebug(LogFilter.Warden, "Ticks {0}", newClientTicks);      // At response
				Log.outDebug(LogFilter.Warden, "Ticks diff {0}", ourTicks - newClientTicks);
			}

			BigInteger      rs;
			WardenCheck     rd;
			WardenCheckType type;
			ushort          checkFailed = 0;

			foreach (var id in _currentChecks)
			{
				WardenCheck check = Global.WardenCheckMgr.GetCheckData(id);

				switch (check.Type)
				{
					case WardenCheckType.Mem:
					{
						byte result = buff.ReadUInt8();

						if (result != 0)
						{
							Log.outDebug(LogFilter.Warden, $"RESULT MEM_CHECK not 0x00, CheckId {id} account Id {_session.GetAccountId()}");
							checkFailed = id;

							continue;
						}

						byte[] expected = Global.WardenCheckMgr.GetCheckResult(id);

						if (buff.ReadBytes((uint)expected.Length).Compare(expected))
						{
							Log.outDebug(LogFilter.Warden, $"RESULT MEM_CHECK fail CheckId {id} account Id {_session.GetAccountId()}");
							checkFailed = id;

							continue;
						}

						Log.outDebug(LogFilter.Warden, $"RESULT MEM_CHECK passed CheckId {id} account Id {_session.GetAccountId()}");

						break;
					}
					case WardenCheckType.PageA:
					case WardenCheckType.PageB:
					case WardenCheckType.Driver:
					case WardenCheckType.Module:
					{
						if (buff.ReadUInt8() != 0xE9)
						{
							Log.outDebug(LogFilter.Warden, $"RESULT {check.Type} fail, CheckId {id} account Id {_session.GetAccountId()}");
							checkFailed = id;

							continue;
						}

						Log.outDebug(LogFilter.Warden, $"RESULT {check.Type} passed CheckId {id} account Id {_session.GetAccountId()}");

						break;
					}
					case WardenCheckType.LuaEval:
					{
						byte result = buff.ReadUInt8();

						if (result == 0)
							buff.Skip(buff.ReadUInt8()); // discard attached string

						Log.outDebug(LogFilter.Warden, $"LUA_EVAL_CHECK CheckId {id} account Id {_session.GetAccountId()} got in-warden dummy response ({result})");

						break;
					}
					case WardenCheckType.Mpq:
					{
						byte result = buff.ReadUInt8();

						if (result != 0)
						{
							Log.outDebug(LogFilter.Warden, $"RESULT MPQ_CHECK not 0x00 account Id {_session.GetAccountId()}", _session.GetAccountId());
							checkFailed = id;

							continue;
						}

						if (!buff.ReadBytes(20).Compare(Global.WardenCheckMgr.GetCheckResult(id))) // SHA1
						{
							Log.outDebug(LogFilter.Warden, $"RESULT MPQ_CHECK fail, CheckId {id} account Id {_session.GetAccountId()}");
							checkFailed = id;

							continue;
						}

						Log.outDebug(LogFilter.Warden, $"RESULT MPQ_CHECK passed, CheckId {id} account Id {_session.GetAccountId()}");

						break;
					}
					default: // Should never happen
						break;
				}
			}

			if (checkFailed > 0)
			{
				WardenCheck check   = Global.WardenCheckMgr.GetCheckData(checkFailed);
				string      penalty = ApplyPenalty(check);
				Log.outWarn(LogFilter.Warden, $"{_session.GetPlayerInfo()} failed Warden check {checkFailed}. Action: {penalty}");
			}

			// Set hold off timer, minimum timer should at least be 1 second
			uint holdOff = WorldConfig.GetUIntValue(WorldCfg.WardenClientCheckHoldoff);
			_checkTimer = (holdOff < 1 ? 1 : holdOff) * Time.InMilliseconds;
		}
	}

	internal class WardenInitModuleRequest
	{
		public uint CheckSumm1;
		public uint CheckSumm2;
		public uint CheckSumm3;
		public WardenOpcodes Command1;

		public WardenOpcodes Command2;

		public WardenOpcodes Command3;
		public uint[] Function1 = new uint[4];
		public uint Function2;
		public byte Function2_set;
		public uint Function3;
		public byte Function3_set;
		public ushort Size1;
		public ushort Size2;
		public ushort Size3;
		public byte String_library1;
		public byte String_library2;
		public byte String_library3;
		public byte Type;
		public byte Unk1;
		public byte Unk2;
		public byte Unk3;
		public byte Unk4;
		public byte Unk5;
		public byte Unk6;

		public static implicit operator byte[](WardenInitModuleRequest request)
		{
			ByteBuffer buffer = new ByteBuffer();
			buffer.WriteUInt8((byte)request.Command1);
			buffer.WriteUInt16(request.Size1);
			buffer.WriteUInt32(request.CheckSumm1);
			buffer.WriteUInt8(request.Unk1);
			buffer.WriteUInt8(request.Unk2);
			buffer.WriteUInt8(request.Type);
			buffer.WriteUInt8(request.String_library1);

			foreach (var function in request.Function1)
				buffer.WriteUInt32(function);

			buffer.WriteUInt8((byte)request.Command2);
			buffer.WriteUInt16(request.Size2);
			buffer.WriteUInt32(request.CheckSumm2);
			buffer.WriteUInt8(request.Unk3);
			buffer.WriteUInt8(request.Unk4);
			buffer.WriteUInt8(request.String_library2);
			buffer.WriteUInt32(request.Function2);
			buffer.WriteUInt8(request.Function2_set);

			buffer.WriteUInt8((byte)request.Command3);
			buffer.WriteUInt16(request.Size3);
			buffer.WriteUInt32(request.CheckSumm3);
			buffer.WriteUInt8(request.Unk5);
			buffer.WriteUInt8(request.Unk6);
			buffer.WriteUInt8(request.String_library3);
			buffer.WriteUInt32(request.Function3);
			buffer.WriteUInt8(request.Function3_set);

			return buffer.GetData();
		}
	}

	internal class CategoryCheck
	{
		public List<ushort> _checks = new();
		public ushort currentIndex;

		public CategoryCheck(List<ushort> checks)
		{
			_checks      = checks;
			currentIndex = 0;
		}

		public bool Empty()
		{
			return _checks.Empty();
		}

		public bool IsAtEnd()
		{
			return currentIndex >= _checks.Count;
		}

		public void Shuffle()
		{
			_checks      = _checks.Shuffle().ToList();
			currentIndex = 0;
		}
	}
}