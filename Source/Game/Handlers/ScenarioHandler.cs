﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.QueryScenarioPoi)]
        void HandleQueryScenarioPOI(QueryScenarioPOI queryScenarioPOI)
        {
            var response = new ScenarioPOIs();

            // Read criteria tree ids and add the in a unordered_set so we don't send POIs for the same criteria tree multiple times
            var criteriaTreeIds = new List<int>();
            for (var i = 0; i < queryScenarioPOI.MissingScenarioPOIs.Count; ++i)
                criteriaTreeIds.Add(queryScenarioPOI.MissingScenarioPOIs[i]); // CriteriaTreeID

            foreach (var criteriaTreeId in criteriaTreeIds)
            {
                var poiVector = Global.ScenarioMgr.GetScenarioPOIs((uint)criteriaTreeId);
                if (poiVector != null)
                {
                    var scenarioPOIData = new ScenarioPOIData();
                    scenarioPOIData.CriteriaTreeID = criteriaTreeId;
                    scenarioPOIData.ScenarioPOIs = poiVector;
                    response.ScenarioPOIDataStats.Add(scenarioPOIData);
                }
            }

            SendPacket(response);
        }
    }
}
