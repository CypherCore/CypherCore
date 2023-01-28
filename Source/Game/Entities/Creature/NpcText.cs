using Framework.Constants;

namespace Game.Misc;

public class NpcText
{
    public NpcTextData[] Data { get; set; } = new NpcTextData[SharedConst.MaxNpcTextOptions];
}