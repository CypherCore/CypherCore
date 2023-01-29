// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Entities
{
    public class AzeriteData
    {
        public List<uint> AzeriteItemMilestonePowers { get; set; } = new();
        public uint KnowledgeLevel { get; set; }
        public uint Level { get; set; }
        public AzeriteItemSelectedEssencesData[] SelectedAzeriteEssences { get; set; } = new AzeriteItemSelectedEssencesData[4];
        public List<AzeriteEssencePowerRecord> UnlockedAzeriteEssences { get; set; } = new();
        public ulong Xp { get; set; }
    }
}