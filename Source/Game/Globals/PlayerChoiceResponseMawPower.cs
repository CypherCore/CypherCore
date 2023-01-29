// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public struct PlayerChoiceResponseMawPower
    {
        public int TypeArtFileID { get; set; }
        public int? Rarity { get; set; }
        public uint? RarityColor { get; set; }
        public int SpellID { get; set; }
        public int MaxStacks { get; set; }
    }
}