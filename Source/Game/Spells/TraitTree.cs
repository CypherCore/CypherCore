using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;

namespace Game
{
    internal class TraitTree
    {
        public TraitConfigType ConfigType { get; set; }
        public List<TraitCostRecord> Costs { get; set; } = new();
        public List<TraitCurrencyRecord> Currencies { get; set; } = new();
        public TraitTreeRecord Data { get; set; }
        public List<TraitNode> Nodes { get; set; } = new();
    }
}