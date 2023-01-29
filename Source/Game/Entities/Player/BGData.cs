// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    // Holder for Battlegrounddata
    public class BGData
    {
        public BGData()
        {
            TypeID = BattlegroundTypeId.None;
            ClearTaxiPath();
            JoinPos = new WorldLocation();
        }

        public byte AfkReportedCount { get; set; }
        public long AfkReportedTimer { get; set; }

        public List<ObjectGuid> AfkReporter { get; set; } = new();

        public uint InstanceID { get; set; } //< This variable is set to bg._InstanceID,

        public uint Team { get; set; } //< What side the player will be added to

        //  when player is teleported to BG - (it is Battleground's GUID)
        public BattlegroundTypeId TypeID { get; set; }

        public WorldLocation JoinPos { get; set; } //< From where player entered BG

        public uint MountSpell { get; set; }
        public uint[] TaxiPath { get; set; } = new uint[2];

        public void ClearTaxiPath()
        {
            TaxiPath[0] = TaxiPath[1] = 0;
        }

        public bool HasTaxiPath()
        {
            return TaxiPath[0] != 0 && TaxiPath[1] != 0;
        }
    }
}