using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Battlepay;
using Game.BattlePets;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Game.AI.SmartAction;
using static Game.ScriptNameContainer;

namespace Game.Networking.Packets.Bpay
{
    public class BpayVisual
    {
        public uint Entry { get; set; }
        public string Name { get; set; } = "";
        public uint DisplayId { get; set; }
        public uint VisualId { get; set; }
        public uint Unk { get; set; }
        public uint DisplayInfoEntry { get; set; }

        public void Write(WorldPacket _worldPacket)
        {
            _worldPacket.WriteBits(Name.Length, 10);
            _worldPacket.FlushBits();
            _worldPacket.Write(DisplayId);
            _worldPacket.Write(VisualId);
            _worldPacket.Write(Unk);
            _worldPacket.WriteString(Name);
        }
    }
}
