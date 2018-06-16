using Game.Maps;
using Game.Scripting;
using Game.Entities;

namespace Scripts.EasternKingdoms.TheStockade
{
    struct Misc
    {
        public static Position WardenThelwaterMovePos = new Position(152.019f, 106.198f, -35.1896f, 1.082104f);
        public static Position WardenThelwaterPos = new Position(138.369f, 78.2932f, -33.85627f, 1.082104f);
    }

    struct DataTypes
    {
        public const uint RandolphMoloch = 0;
        public const uint LordOverheat = 1;
        public const uint Hogger = 2;
    }

    struct CreatureIds
    {
        public const uint RandolphMoloch = 46383;
        public const uint LordOverheat = 46264;
        public const uint Hogger = 46254;
        public const uint WardenThelwater = 46409;
        public const uint MortimerMoloch = 46482;
    }



    [Script]
    class instance_the_stockade : InstanceMapScript
    {
        public instance_the_stockade() : base("instance_the_stockade", 34) { }

        class instance_the_stockade_InstanceMapScript : InstanceScript
        {
            public instance_the_stockade_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("SS");
                SetBossNumber(3);
            }
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_the_stockade_InstanceMapScript(map);
        }
    }
}
