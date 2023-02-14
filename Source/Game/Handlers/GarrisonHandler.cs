// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Garrisons;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.GetGarrisonInfo)]
        void HandleGetGarrisonInfo(GetGarrisonInfo getGarrisonInfo)
        {
            Garrison garrison = _player.GetGarrison();
            if (garrison != null)
                garrison.SendInfo();
        }

        [WorldPacketHandler(ClientOpcodes.GarrisonPurchaseBuilding)]
        void HandleGarrisonPurchaseBuilding(GarrisonPurchaseBuilding garrisonPurchaseBuilding)
        {
            if (!_player.GetNPCIfCanInteractWith(garrisonPurchaseBuilding.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
                return;

            Garrison garrison = _player.GetGarrison();
            if (garrison != null)
                garrison.PlaceBuilding(garrisonPurchaseBuilding.PlotInstanceID, garrisonPurchaseBuilding.BuildingID);
        }

        [WorldPacketHandler(ClientOpcodes.GarrisonCancelConstruction)]
        void HandleGarrisonCancelConstruction(GarrisonCancelConstruction garrisonCancelConstruction)
        {
            if (!_player.GetNPCIfCanInteractWith(garrisonCancelConstruction.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
                return;

            Garrison garrison = _player.GetGarrison();
            if (garrison != null)
                garrison.CancelBuildingConstruction(garrisonCancelConstruction.PlotInstanceID);
        }

        [WorldPacketHandler(ClientOpcodes.GarrisonRequestBlueprintAndSpecializationData)]
        void HandleGarrisonRequestBlueprintAndSpecializationData(GarrisonRequestBlueprintAndSpecializationData garrisonRequestBlueprintAndSpecializationData)
        {
            Garrison garrison = _player.GetGarrison();
            if (garrison != null)
                garrison.SendBlueprintAndSpecializationData();
        }

        [WorldPacketHandler(ClientOpcodes.GarrisonGetMapData)]
        void HandleGarrisonGetMapData(GarrisonGetMapData garrisonGetMapData)
        {
            Garrison garrison = _player.GetGarrison();
            if (garrison != null)
                garrison.SendMapData(_player);
        }
    }
}
