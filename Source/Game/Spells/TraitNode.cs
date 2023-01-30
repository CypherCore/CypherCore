using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;

namespace Game
{
    internal class TraitNode
    {
        public List<TraitCondRecord> Conditions { get; set; } = new();
        public List<TraitCostRecord> Costs { get; set; } = new();
        public TraitNodeRecord Data { get; set; }
        public List<TraitNodeEntry> Entries { get; set; } = new();
        public List<TraitNodeGroup> Groups { get; set; } = new();
        public List<Tuple<TraitNode, TraitEdgeType>> ParentNodes { get; set; } = new(); // TraitEdge::LeftTraitNodeID
    }
}