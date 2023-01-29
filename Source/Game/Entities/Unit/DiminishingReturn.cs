using Framework.Constants;

namespace Game.Entities;

public struct DiminishingReturn
{
    public DiminishingReturn(uint hitTime, DiminishingLevels hitCount)
    {
        Stack = 0;
        HitTime = hitTime;
        HitCount = hitCount;
    }

    public void Clear()
    {
        Stack = 0;
        HitTime = 0;
        HitCount = DiminishingLevels.Level1;
    }

    public uint Stack { get; set; }
    public uint HitTime { get; set; }
    public DiminishingLevels HitCount { get; set; }
}