using System.Collections.Generic;
using Game.Conditions;

namespace Game.Misc;

public class GossipMenus
{
    public List<Condition> Conditions { get; set; } = new();
    public uint MenuId { get; set; }
    public uint TextId { get; set; }
}