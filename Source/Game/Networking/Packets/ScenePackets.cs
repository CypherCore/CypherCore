// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    class PlayScene : ServerPacket
    {
        public PlayScene() : base(ServerOpcodes.PlayScene, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneID);
            _worldPacket.WriteUInt32(PlaybackFlags);
            _worldPacket.WriteUInt32(SceneInstanceID);
            _worldPacket.WriteUInt32(SceneScriptPackageID);
            _worldPacket.WritePackedGuid(TransportGUID);
            _worldPacket.WriteXYZO(Location);
            _worldPacket.WriteBit(Encrypted);
            _worldPacket.FlushBits();
        }

        public uint SceneID;
        public uint PlaybackFlags;
        public uint SceneInstanceID;
        public uint SceneScriptPackageID;
        public ObjectGuid TransportGUID;
        public Position Location;
        public bool Encrypted;
    }

    class CancelScene : ServerPacket
    {
        public CancelScene() : base(ServerOpcodes.CancelScene, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SceneInstanceID);
        }

        public uint SceneInstanceID;
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

        public uint SceneInstanceID;
        public string _Event;
    }

    class ScenePlaybackComplete : ClientPacket
    {
        public ScenePlaybackComplete(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }

        public uint SceneInstanceID;
    }

    class ScenePlaybackCanceled : ClientPacket
    {
        public ScenePlaybackCanceled(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SceneInstanceID = _worldPacket.ReadUInt32();
        }

        public uint SceneInstanceID;
    }
}
