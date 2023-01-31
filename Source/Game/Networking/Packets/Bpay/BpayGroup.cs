namespace Game.Networking.Packets.Bpay
{
    public class BpayGroup
    {
        public uint Entry { get; set; }
        public uint GroupId { get; set; }
        public uint IconFileDataID { get; set; }
        public byte DisplayType { get; set; }
        public uint Ordering { get; set; }
        public uint Unk { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.Write(GroupId);
            _worldPacket.Write(IconFileDataID);
            _worldPacket.Write(DisplayType);
            _worldPacket.Write(Ordering);
            _worldPacket.Write(Unk);
            _worldPacket.WriteBits(Name.Length, 8);
            _worldPacket.WriteBits(Description.Length + 1, 24);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Name);

            if (!string.IsNullOrEmpty(Description))
                _worldPacket.Write(Description);
        }
    }


}
