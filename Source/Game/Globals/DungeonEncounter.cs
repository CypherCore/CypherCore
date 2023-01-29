// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;

namespace Game
{
    public class DungeonEncounter
    {
        public uint CreditEntry { get; set; }
        public EncounterCreditType CreditType { get; set; }

        public DungeonEncounterRecord DbcEntry { get; set; }
        public uint LastEncounterDungeon { get; set; }

        public DungeonEncounter(DungeonEncounterRecord _dbcEntry, EncounterCreditType _creditType, uint _creditEntry, uint _lastEncounterDungeon)
        {
            DbcEntry = _dbcEntry;
            CreditType = _creditType;
            CreditEntry = _creditEntry;
            LastEncounterDungeon = _lastEncounterDungeon;
        }
    }
}