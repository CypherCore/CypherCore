// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.BattleGrounds.Zones.StrandofAncients
{
    internal enum SAGateState
    {
        // alliance is defender
        AllianceGateOk = 1,
        AllianceGateDamaged = 2,
        AllianceGateDestroyed = 3,

        // horde is defender
        HordeGateOk = 4,
        HordeGateDamaged = 5,
        HordeGateDestroyed = 6
    }

}