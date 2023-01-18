// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.Entities;

namespace Game.Networking.Packets
{
    public class AccountDataTimes : ServerPacket
    {
        public AccountDataTimes() : base(ServerOpcodes.AccountDataTimes) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGuid);
            _worldPacket.WriteInt64(ServerTime);
            foreach (var accounttime in AccountTimes)
                _worldPacket.WriteInt64(accounttime);
        }

        public ObjectGuid PlayerGuid;
        public long ServerTime;
        public long[] AccountTimes = new long[(int)AccountDataTypes.Max];
    }

    public class ClientCacheVersion : ServerPacket
    {
        public ClientCacheVersion() : base(ServerOpcodes.CacheVersion) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CacheVersion);
        }

        public uint CacheVersion = 0;
    }

    public class RequestAccountData : ClientPacket
    {
        public RequestAccountData(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PlayerGuid = _worldPacket.ReadPackedGuid();
            DataType = (AccountDataTypes)_worldPacket.ReadBits<uint>(4);
        }

        public ObjectGuid PlayerGuid;
        public AccountDataTypes DataType = 0;
    }

    public class UpdateAccountData : ServerPacket
    {
        public UpdateAccountData() : base(ServerOpcodes.UpdateAccountData) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WriteInt64(Time);
            _worldPacket.WriteUInt32(Size);
            _worldPacket.WriteBits(DataType, 4);

            if (CompressedData == null)
                _worldPacket.WriteUInt32(0);
            else
            {
                var bytes = CompressedData.GetData();
                _worldPacket.WriteInt32(bytes.Length);
                _worldPacket.WriteBytes(bytes);
            }
        }

        public ObjectGuid Player;
        public long Time; // UnixTime
        public uint Size; // decompressed size
        public AccountDataTypes DataType = 0;
        public ByteBuffer CompressedData;
    }

    public class UserClientUpdateAccountData : ClientPacket
    {
        public UserClientUpdateAccountData(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PlayerGuid = _worldPacket.ReadPackedGuid();
            Time = _worldPacket.ReadInt64();
            Size = _worldPacket.ReadUInt32();
            DataType = (AccountDataTypes)_worldPacket.ReadBits<uint>(4);

            uint compressedSize = _worldPacket.ReadUInt32();
            if (compressedSize != 0)
            {
                CompressedData = new ByteBuffer(_worldPacket.ReadBytes(compressedSize));
            }
        }

        public ObjectGuid PlayerGuid;
        public long Time; // UnixTime
        public uint Size; // decompressed size
        public AccountDataTypes DataType = 0;
        public ByteBuffer CompressedData;
    }

    class SetAdvancedCombatLogging : ClientPacket
    {
        public SetAdvancedCombatLogging(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Enable = _worldPacket.HasBit();
        }

        public bool Enable;
    }
}
