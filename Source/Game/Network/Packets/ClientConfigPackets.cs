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
using Framework.IO;
using Game.Entities;

namespace Game.Network.Packets
{
    public class AccountDataTimes : ServerPacket
    {
        public AccountDataTimes() : base(ServerOpcodes.AccountDataTimes) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PlayerGuid);
            _worldPacket.WriteUInt32(ServerTime);
            for (int i = 0; i < (int)AccountDataTypes.Max; ++i)
                _worldPacket.WriteUInt32(AccountTimes[i]);
        }

        public ObjectGuid PlayerGuid;
        public uint ServerTime = 0;
        public uint[] AccountTimes = new uint[(int)AccountDataTypes.Max];
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
            DataType = (AccountDataTypes)_worldPacket.ReadBits<uint>(3);
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
            _worldPacket.WriteUInt32(Time);
            _worldPacket.WriteUInt32(Size);
            _worldPacket.WriteBits(DataType, 3);

            var bytes = CompressedData.GetData();
            _worldPacket.WriteUInt32(bytes.Length);
            _worldPacket.WriteBytes(bytes);
        }

        public ObjectGuid Player;
        public uint Time = 0; // UnixTime
        public uint Size = 0; // decompressed size
        public AccountDataTypes DataType = 0;
        public ByteBuffer CompressedData;
    }

    public class UserClientUpdateAccountData : ClientPacket
    {
        public UserClientUpdateAccountData(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PlayerGuid = _worldPacket.ReadPackedGuid();
            Time = _worldPacket.ReadUInt32();
            Size = _worldPacket.ReadUInt32();
            DataType = (AccountDataTypes)_worldPacket.ReadBits<uint>(3);

            uint compressedSize = _worldPacket.ReadUInt32();
            if (compressedSize != 0)
            {
                CompressedData = new ByteBuffer(_worldPacket.ReadBytes(compressedSize));
            }
        }

        public ObjectGuid PlayerGuid;
        public uint Time; // UnixTime
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
