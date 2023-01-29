// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class AccessRequirement
    {
        public uint Achievement { get; set; }
        public uint Item { get; set; }
        public uint Item2 { get; set; }
        public byte LevelMax { get; set; }
        public byte LevelMin { get; set; }
        public uint Quest_A { get; set; }
        public uint Quest_H { get; set; }
        public string QuestFailedText { get; set; }
    }
}