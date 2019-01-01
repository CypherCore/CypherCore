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

using Framework.Constants;
using Game.Garrisons;
using Game.Network;
using Game.Network.Packets;

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
            if (!_player.GetNPCIfCanInteractWith(garrisonPurchaseBuilding.NpcGUID, NPCFlags.GarrisonArchitect))
                return;

            Garrison garrison = _player.GetGarrison();
            if (garrison != null)
                garrison.PlaceBuilding(garrisonPurchaseBuilding.PlotInstanceID, garrisonPurchaseBuilding.BuildingID);
        }

        [WorldPacketHandler(ClientOpcodes.GarrisonCancelConstruction)]
        void HandleGarrisonCancelConstruction(GarrisonCancelConstruction garrisonCancelConstruction)
        {
            if (!_player.GetNPCIfCanInteractWith(garrisonCancelConstruction.NpcGUID, NPCFlags.GarrisonArchitect))
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

        [WorldPacketHandler(ClientOpcodes.GarrisonGetBuildingLandmarks)]
        void HandleGarrisonGetBuildingLandmarks(GarrisonGetBuildingLandmarks garrisonGetBuildingLandmarks)
        {
            Garrison garrison = _player.GetGarrison();
            if (garrison != null)
                garrison.SendBuildingLandmarks(_player);
        }
    }
}
