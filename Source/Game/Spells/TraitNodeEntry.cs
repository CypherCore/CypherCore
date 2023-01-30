using System.Collections.Generic;
using Game.DataStorage;

namespace Game
{
    internal class TraitNodeEntry
    {
        public List<TraitCondRecord> Conditions { get; set; } = new();
        public List<TraitCostRecord> Costs { get; set; } = new();
        public TraitNodeEntryRecord Data { get; set; }
    }
}