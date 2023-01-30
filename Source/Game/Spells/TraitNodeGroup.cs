using System.Collections.Generic;
using Game.DataStorage;

namespace Game
{
    internal class TraitNodeGroup
    {
        public List<TraitCondRecord> Conditions { get; set; } = new();
        public List<TraitCostRecord> Costs { get; set; } = new();
        public TraitNodeGroupRecord Data { get; set; }
        public List<TraitNode> Nodes { get; set; } = new();
    }
}