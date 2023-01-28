using Framework.Constants;

namespace Game.Misc;

public class GossipMenuItem
{
    public uint Action { get; set; }

    // Action _data
    public uint ActionMenuID { get; set; }
    public uint ActionPoiID { get; set; }
    public bool BoxCoded { get; set; }
    public uint BoxMoney { get; set; }
    public string BoxText { get; set; }
    public GossipOptionFlags Flags { get; set; }
    public int? GossipNpcOptionID { get; set; }
    public int GossipOptionID { get; set; }
    public uint Language { get; set; }
    public GossipOptionNpc OptionNpc { get; set; }
    public string OptionText { get; set; }
    public uint OrderIndex { get; set; }
    public int? OverrideIconID { get; set; }

    // additional scripting identifiers
    public uint Sender { get; set; }
    public int? SpellID { get; set; }
}