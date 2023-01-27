// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;

namespace Game.Networking.Packets
{
	public class AccountDataTimes : ServerPacket
	{
		public long[] AccountTimes = new long[(int)AccountDataTypes.Max];

		public ObjectGuid PlayerGuid;
		public long ServerTime;

		public AccountDataTimes() : base(ServerOpcodes.AccountDataTimes)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(PlayerGuid);
			_worldPacket.WriteInt64(ServerTime);

			foreach (var accounttime in AccountTimes)
				_worldPacket.WriteInt64(accounttime);
		}
	}

	public class ClientCacheVersion : ServerPacket
	{
		public uint CacheVersion = 0;

		public ClientCacheVersion() : base(ServerOpcodes.CacheVersion)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(CacheVersion);
		}
	}

	public class RequestAccountData : ClientPacket
	{
		public AccountDataTypes DataType = 0;

		public ObjectGuid PlayerGuid;

		public RequestAccountData(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PlayerGuid = _worldPacket.ReadPackedGuid();
			DataType   = (AccountDataTypes)_worldPacket.ReadBits<uint>(4);
		}
	}

	public class UpdateAccountData : ServerPacket
	{
		public ByteBuffer CompressedData;
		public AccountDataTypes DataType = 0;

		public ObjectGuid Player;
		public uint Size; // decompressed size
		public long Time; // UnixTime

		public UpdateAccountData() : base(ServerOpcodes.UpdateAccountData)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(Player);
			_worldPacket.WriteInt64(Time);
			_worldPacket.WriteUInt32(Size);
			_worldPacket.WriteBits(DataType, 4);

			if (CompressedData == null)
			{
				_worldPacket.WriteUInt32(0);
			}
			else
			{
				var bytes = CompressedData.GetData();
				_worldPacket.WriteInt32(bytes.Length);
				_worldPacket.WriteBytes(bytes);
			}
		}
	}

	public class UserClientUpdateAccountData : ClientPacket
	{
		public ByteBuffer CompressedData;
		public AccountDataTypes DataType = 0;

		public ObjectGuid PlayerGuid;
		public uint Size; // decompressed size
		public long Time; // UnixTime

		public UserClientUpdateAccountData(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PlayerGuid = _worldPacket.ReadPackedGuid();
			Time       = _worldPacket.ReadInt64();
			Size       = _worldPacket.ReadUInt32();
			DataType   = (AccountDataTypes)_worldPacket.ReadBits<uint>(4);

			uint compressedSize = _worldPacket.ReadUInt32();

			if (compressedSize != 0)
				CompressedData = new ByteBuffer(_worldPacket.ReadBytes(compressedSize));
		}
	}

	internal class SetAdvancedCombatLogging : ClientPacket
	{
		public bool Enable;

		public SetAdvancedCombatLogging(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Enable = _worldPacket.HasBit();
		}
	}
}