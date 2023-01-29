// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.IO;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities
{
    public class UpdateData
    {
        private uint _blockCount;
        private readonly ByteBuffer _data = new();
        private readonly List<ObjectGuid> _destroyGUIDs = new();
        private uint _mapId;
        private readonly List<ObjectGuid> _outOfRangeGUIDs = new();

        public UpdateData(uint mapId)
        {
            _mapId = mapId;
        }

        public void AddDestroyObject(ObjectGuid guid)
        {
            _destroyGUIDs.Add(guid);
        }

        public void AddOutOfRangeGUID(List<ObjectGuid> guids)
        {
            _outOfRangeGUIDs.AddRange(guids);
        }

        public void AddOutOfRangeGUID(ObjectGuid guid)
        {
            _outOfRangeGUIDs.Add(guid);
        }

        public void AddUpdateBlock(ByteBuffer block)
        {
            _data.WriteBytes(block.GetData());
            ++_blockCount;
        }

        public bool BuildPacket(out UpdateObject packet)
        {
            packet = new UpdateObject();

            packet.NumObjUpdates = _blockCount;
            packet.MapID = (ushort)_mapId;

            WorldPacket buffer = new();

            if (buffer.WriteBit(!_outOfRangeGUIDs.Empty() || !_destroyGUIDs.Empty()))
            {
                buffer.WriteUInt16((ushort)_destroyGUIDs.Count);
                buffer.WriteInt32(_destroyGUIDs.Count + _outOfRangeGUIDs.Count);

                foreach (var destroyGuid in _destroyGUIDs)
                    buffer.WritePackedGuid(destroyGuid);

                foreach (var outOfRangeGuid in _outOfRangeGUIDs)
                    buffer.WritePackedGuid(outOfRangeGuid);
            }

            var bytes = _data.GetData();
            buffer.WriteInt32(bytes.Length);
            buffer.WriteBytes(bytes);

            packet.Data = buffer.GetData();

            return true;
        }

        public void Clear()
        {
            _data.Clear();
            _destroyGUIDs.Clear();
            _outOfRangeGUIDs.Clear();
            _blockCount = 0;
            _mapId = 0;
        }

        public bool HasData()
        {
            return _blockCount > 0 || !_outOfRangeGUIDs.Empty() || !_destroyGUIDs.Empty();
        }

        public List<ObjectGuid> GetOutOfRangeGUIDs()
        {
            return _outOfRangeGUIDs;
        }

        public void SetMapId(ushort mapId)
        {
            _mapId = mapId;
        }
    }
}