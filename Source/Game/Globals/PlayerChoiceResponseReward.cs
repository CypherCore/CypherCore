// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class PlayerChoiceResponseReward
    {
        public uint ArenaPointCount { get; set; }
        public List<PlayerChoiceResponseRewardEntry> Currency { get; set; } = new();
        public List<PlayerChoiceResponseRewardEntry> Faction { get; set; } = new();
        public uint HonorPointCount { get; set; }
        public List<PlayerChoiceResponseRewardItem> ItemChoices { get; set; } = new();

        public List<PlayerChoiceResponseRewardItem> Items { get; set; } = new();
        public ulong Money { get; set; }
        public int PackageId { get; set; }
        public int SkillLineId { get; set; }
        public uint SkillPointCount { get; set; }
        public int TitleId { get; set; }
        public uint Xp { get; set; }
    }
}