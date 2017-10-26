/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;

namespace Game.Network.Packets
{
    class PlayScene : ServerPacket
    {
        public PlayScene() : base(ServerOpcodes.PlayScene) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneID);
            _worldPacket.WriteUInt32(PlaybackFlags);
            _worldPacket.WriteUInt32(SceneInstanceID);
            _worldPacket.WriteUInt32(SceneScriptPackageID);
            _worldPacket.WritePackedGuid(TransportGUID);
            _worldPacket.WriteXYZO(Location);
        }

        public uint SceneID { get; set; }
        public uint PlaybackFlags { get; set; }
        public uint SceneInstanceID { get; set; }
        public uint SceneScriptPackageID { get; set; }
        public ObjectGuid TransportGUID { get; set; }
        public Position Location { get; set; }
    }

    class CancelScene : ServerPacket
    {
        public CancelScene() : base(ServerOpcodes.CancelScene) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneInstanceID);
        }

        public uint SceneInstanceID { get; set; }
    }

    class SceneTriggerEvent : ClientPacket
    {
        public SceneTriggerEvent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint len = _worldPacket.ReadBits<uint>(6);
            SceneInstanceID = _worldPacket.ReadUInt32();
            _Event = _worldPacket.ReadString(len);
        }

        public uint SceneInstanceID { get; set; }
        public string _Event { get; set; }
    }

    class ScenePlaybackComplete : ClientPacket
    {
        public ScenePlaybackComplete(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }

        public uint SceneInstanceID { get; set; }
    }

    class ScenePlaybackCanceled : ClientPacket
    {
        public ScenePlaybackCanceled(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }

        public uint SceneInstanceID { get; set; }
    }
}
