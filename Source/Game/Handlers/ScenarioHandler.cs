// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.QueryScenarioPoi, Processing = PacketProcessing.Inplace)]
        void HandleQueryScenarioPOI(QueryScenarioPOI queryScenarioPOI)
        {
            ScenarioPOIs response = new();

            // Read criteria tree ids and add the in a unordered_set so we don't send POIs for the same criteria tree multiple times
            List<int> criteriaTreeIds = new();
            for (int i = 0; i < queryScenarioPOI.MissingScenarioPOIs.Count; ++i)
                criteriaTreeIds.Add(queryScenarioPOI.MissingScenarioPOIs[i]); // CriteriaTreeID

            foreach (int criteriaTreeId in criteriaTreeIds)
            {
                var poiVector = Global.ScenarioMgr.GetScenarioPOIs((uint)criteriaTreeId);
                if (poiVector != null)
                {
                    ScenarioPOIData scenarioPOIData = new();
                    scenarioPOIData.CriteriaTreeID = criteriaTreeId;
                    scenarioPOIData.ScenarioPOIs = poiVector;
                    response.ScenarioPOIDataStats.Add(scenarioPOIData);
                }
            }

            SendPacket(response);
        }
    }
}
