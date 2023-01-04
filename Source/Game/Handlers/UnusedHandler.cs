using Bgs.Protocol;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.BattlePayGetPurchaseList, Processing = PacketProcessing.Inplace)]
        [WorldPacketHandler(ClientOpcodes.BattlePayGetProductList, Processing = PacketProcessing.Inplace)]
        [WorldPacketHandler(ClientOpcodes.UpdateVasPurchaseStates, Processing = PacketProcessing.Inplace)]
        [WorldPacketHandler(ClientOpcodes.Unknown_3743, Processing = PacketProcessing.Inplace)]
        [WorldPacketHandler(ClientOpcodes.OverrideScreenFlash, Processing = PacketProcessing.Inplace)]
        [WorldPacketHandler(ClientOpcodes.GetAccountCharacterList, Processing = PacketProcessing.Inplace)]
        [WorldPacketHandler(ClientOpcodes.QueuedMessagesEnd, Processing = PacketProcessing.Inplace)]
        void HandleUnused(EmptyPaket empty) { }        
    }
}
