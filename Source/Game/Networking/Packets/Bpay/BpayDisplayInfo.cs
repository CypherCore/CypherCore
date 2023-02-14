// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets.Bpay
{
    public class BpayDisplayInfo
    {
        public uint Entry { get; set; }
        public uint CreatureDisplayID { get; set; }
        public uint VisualID { get; set; }
        public string Name1 { get; set; } = "";
        public string Name2 { get; set; } = "";
        public string Name3 { get; set; } = "";
        public string Name4 { get; set; } = "";
        public string Name5 { get; set; } = "";
        public string Name6 { get; set; } = "";
        public string Name7 { get; set; } = "";
        public uint Flags { get; set; }
        public uint Unk1 { get; set; }
        public uint Unk2 { get; set; }
        public uint Unk3 { get; set; } // UiTextureAtlasMemberID
        public uint UnkInt1 { get; set; }
        public uint UnkInt2 { get; set; }
        public uint UnkInt3 { get; set; }

        public List<BpayVisual> Visuals { get; set; } = new List<BpayVisual>();

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.WriteBit(CreatureDisplayID != 0);
            _worldPacket.WriteBit(VisualID != 0);
            _worldPacket.WriteBits(Name1.Length, 10);
            _worldPacket.WriteBits(Name2.Length, 10);
            _worldPacket.WriteBits(Name3.Length, 13);
            _worldPacket.WriteBits(Name4.Length, 13);
            _worldPacket.WriteBits(Name5.Length, 13);
            _worldPacket.WriteBit(Flags != 0);
            _worldPacket.WriteBit(Unk1 != 0);
            _worldPacket.WriteBit(Unk2 != 0);
            _worldPacket.WriteBit(Unk3 != 0);
            _worldPacket.WriteBits(Name6.Length, 13);
            _worldPacket.WriteBits(Name7.Length, 13);
            _worldPacket.FlushBits();
            _worldPacket.Write((uint)Visuals.Count);
            _worldPacket.Write(UnkInt1);
            _worldPacket.Write(UnkInt2);
            _worldPacket.Write(UnkInt3);

            if (CreatureDisplayID != 0)
                _worldPacket.Write(CreatureDisplayID);
            if (VisualID != 0)
                _worldPacket.Write(VisualID);
            _worldPacket.WriteString(Name1);
            _worldPacket.WriteString(Name2);
            _worldPacket.WriteString(Name3);
            _worldPacket.WriteString(Name4);
            _worldPacket.WriteString(Name5);

            if (Flags != 0)
                _worldPacket.Write(Flags);

            if (Unk1 != 0)
                _worldPacket.Write(Unk1);

            if (Unk2 != 0)
                _worldPacket.Write(Unk2);

            if (Unk3 != 0)
                _worldPacket.Write(Unk3);

            _worldPacket.WriteString(Name6);
            _worldPacket.WriteString(Name7);

            foreach (var visual in Visuals)
                visual.Write(_worldPacket);

        }
    }

}
