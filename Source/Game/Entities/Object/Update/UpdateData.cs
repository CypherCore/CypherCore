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

using Framework.IO;
using Game.Network;
using Game.Network.Packets;
using System.Collections.Generic;

namespace Game.Entities
{
    public class UpdateData
    {
        public UpdateData(uint mapId)
        {
            MapId = mapId;
        }

        public void AddOutOfRangeGUID(List<ObjectGuid> guids)
        {
            outOfRangeGUIDs.AddRange(guids);
        }

        public void AddOutOfRangeGUID(ObjectGuid guid)
        {
            outOfRangeGUIDs.Add(guid);
        }

        public void AddUpdateBlock(ByteBuffer block)
        {
            data.WriteBytes(block.GetData());
            ++BlockCount;
        }

        public bool BuildPacket(out UpdateObject packet)
        {
            packet = new UpdateObject();

            packet.NumObjUpdates = BlockCount;
            packet.MapID = (ushort)MapId;

            WorldPacket buffer = new WorldPacket();
            if (buffer.WriteBit(!outOfRangeGUIDs.Empty()))
            {
                buffer.WriteUInt16(0); // object limit to instantly destroy - objects before this index on m_outOfRangeGUIDs list get "smoothly phased out"
                buffer.WriteUInt32(outOfRangeGUIDs.Count);

                foreach (var guid in outOfRangeGUIDs)
                    buffer.WritePackedGuid(guid);
            }
            var bytes = data.GetData();
            buffer.WriteUInt32(bytes.Length);
            buffer.WriteBytes(bytes);

            packet.Data = buffer.GetData();
            return true;
        }

        public void Clear()
        {
            data.Clear();
            outOfRangeGUIDs.Clear();
            BlockCount = 0;
            MapId = 0;
        }

        public bool HasData() { return BlockCount > 0 || outOfRangeGUIDs.Count != 0; }

        public List<ObjectGuid> GetOutOfRangeGUIDs() { return outOfRangeGUIDs; }

        public void SetMapId(ushort mapId) { MapId = mapId; }

        uint MapId;
        uint BlockCount;
        List<ObjectGuid> outOfRangeGUIDs = new List<ObjectGuid>();
        ByteBuffer data = new ByteBuffer();
    }
}
