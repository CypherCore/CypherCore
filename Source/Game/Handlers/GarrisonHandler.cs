// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Garrisons;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
	public partial class WorldSession
	{
		[WorldPacketHandler(ClientOpcodes.GetGarrisonInfo)]
		private void HandleGetGarrisonInfo(GetGarrisonInfo getGarrisonInfo)
		{
			Garrison garrison = _player.GetGarrison();

			if (garrison != null)
				garrison.SendInfo();
		}

		[WorldPacketHandler(ClientOpcodes.GarrisonPurchaseBuilding)]
		private void HandleGarrisonPurchaseBuilding(GarrisonPurchaseBuilding garrisonPurchaseBuilding)
		{
			if (!_player.GetNPCIfCanInteractWith(garrisonPurchaseBuilding.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
				return;

			Garrison garrison = _player.GetGarrison();

			if (garrison != null)
				garrison.PlaceBuilding(garrisonPurchaseBuilding.PlotInstanceID, garrisonPurchaseBuilding.BuildingID);
		}

		[WorldPacketHandler(ClientOpcodes.GarrisonCancelConstruction)]
		private void HandleGarrisonCancelConstruction(GarrisonCancelConstruction garrisonCancelConstruction)
		{
			if (!_player.GetNPCIfCanInteractWith(garrisonCancelConstruction.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
				return;

			Garrison garrison = _player.GetGarrison();

			if (garrison != null)
				garrison.CancelBuildingConstruction(garrisonCancelConstruction.PlotInstanceID);
		}

		[WorldPacketHandler(ClientOpcodes.GarrisonRequestBlueprintAndSpecializationData)]
		private void HandleGarrisonRequestBlueprintAndSpecializationData(GarrisonRequestBlueprintAndSpecializationData garrisonRequestBlueprintAndSpecializationData)
		{
			Garrison garrison = _player.GetGarrison();

			if (garrison != null)
				garrison.SendBlueprintAndSpecializationData();
		}

		[WorldPacketHandler(ClientOpcodes.GarrisonGetMapData)]
		private void HandleGarrisonGetMapData(GarrisonGetMapData garrisonGetMapData)
		{
			Garrison garrison = _player.GetGarrison();

			if (garrison != null)
				garrison.SendMapData(_player);
		}
	}
}