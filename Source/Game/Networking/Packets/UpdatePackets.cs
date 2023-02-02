// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Networking.Packets
{
    public class UpdateObject : ServerPacket
    {
        public UpdateObject() : base(ServerOpcodes.UpdateObject, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(NumObjUpdates);
            _worldPacket.WriteUInt16(MapID);
            _worldPacket.WriteBytes(Data);
        }

        public uint NumObjUpdates;
        public ushort MapID;
        public byte[] Data;
    }
}
