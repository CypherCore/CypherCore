using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;

namespace Game.Misc;

public class GossipMenuItems
{
    public uint ActionMenuID { get; set; }
    public uint ActionPoiID { get; set; }
    public uint BoxBroadcastTextId { get; set; }
    public bool BoxCoded { get; set; }
    public uint BoxMoney { get; set; }
    public string BoxText { get; set; }
    public List<Condition> Conditions { get; set; } = new();
    public GossipOptionFlags Flags { get; set; }
    public int? GossipNpcOptionID { get; set; }
    public int GossipOptionID { get; set; }
    public uint Language { get; set; }
    public uint MenuID { get; set; }
    public uint OptionBroadcastTextId { get; set; }
    public GossipOptionNpc OptionNpc { get; set; }
    public string OptionText { get; set; }
    public uint OrderIndex { get; set; }
    public int? OverrideIconID { get; set; }
    public int? SpellID { get; set; }
}