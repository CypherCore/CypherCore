// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Runtime.InteropServices;

namespace Game.Miscellaneous
{
    public struct RaceMask
    {
        public static RaceMask<ulong> AllPlayable = new RaceMask<ulong>((ulong)(
            RaceMask<ulong>.GetMaskForRace(Race.Human) | RaceMask<ulong>.GetMaskForRace(Race.Orc) | RaceMask<ulong>.GetMaskForRace(Race.Dwarf) | RaceMask<ulong>.GetMaskForRace(Race.NightElf) |
            RaceMask<ulong>.GetMaskForRace(Race.Undead) | RaceMask<ulong>.GetMaskForRace(Race.Tauren) | RaceMask<ulong>.GetMaskForRace(Race.Gnome) | RaceMask<ulong>.GetMaskForRace(Race.Troll) |
            RaceMask<ulong>.GetMaskForRace(Race.BloodElf) | RaceMask<ulong>.GetMaskForRace(Race.Draenei) | RaceMask<ulong>.GetMaskForRace(Race.Goblin) | RaceMask<ulong>.GetMaskForRace(Race.Worgen) |
            RaceMask<ulong>.GetMaskForRace(Race.PandarenNeutral) | RaceMask<ulong>.GetMaskForRace(Race.PandarenAlliance) | RaceMask<ulong>.GetMaskForRace(Race.PandarenHorde) | RaceMask<ulong>.GetMaskForRace(Race.Nightborne) |
            RaceMask<ulong>.GetMaskForRace(Race.HighmountainTauren) | RaceMask<ulong>.GetMaskForRace(Race.VoidElf) | RaceMask<ulong>.GetMaskForRace(Race.LightforgedDraenei) | RaceMask<ulong>.GetMaskForRace(Race.ZandalariTroll) |
            RaceMask<ulong>.GetMaskForRace(Race.KulTiran) | RaceMask<ulong>.GetMaskForRace(Race.DarkIronDwarf) | RaceMask<ulong>.GetMaskForRace(Race.Vulpera) | RaceMask<ulong>.GetMaskForRace(Race.MagharOrc) |
            RaceMask<ulong>.GetMaskForRace(Race.MechaGnome) | RaceMask<ulong>.GetMaskForRace(Race.DracthyrAlliance) | RaceMask<ulong>.GetMaskForRace(Race.DracthyrHorde)));
        
        public static RaceMask<ulong> Neutral = new RaceMask<ulong>((ulong)RaceMask<ulong>.GetMaskForRace(Race.PandarenNeutral));

        public static RaceMask<ulong> Alliance = new RaceMask<ulong>((ulong)(
           RaceMask<ulong>.GetMaskForRace(Race.Human) | RaceMask<ulong>.GetMaskForRace(Race.Dwarf) | RaceMask<ulong>.GetMaskForRace(Race.NightElf) |
           RaceMask<ulong>.GetMaskForRace(Race.Gnome) | RaceMask<ulong>.GetMaskForRace(Race.Draenei) | RaceMask<ulong>.GetMaskForRace(Race.Worgen) |
           RaceMask<ulong>.GetMaskForRace(Race.PandarenAlliance) | RaceMask<ulong>.GetMaskForRace(Race.VoidElf) | RaceMask<ulong>.GetMaskForRace(Race.LightforgedDraenei) |
           RaceMask<ulong>.GetMaskForRace(Race.KulTiran) | RaceMask<ulong>.GetMaskForRace(Race.DarkIronDwarf) | RaceMask<ulong>.GetMaskForRace(Race.MechaGnome) | RaceMask<ulong>.GetMaskForRace(Race.DracthyrAlliance)));

        public static RaceMask<ulong> Horde = new RaceMask<ulong>((ulong)(AllPlayable.RawValue & (~(Neutral | Alliance).RawValue)));
    }

    public struct RaceMask<T>
    {
        public dynamic RawValue;

        public RaceMask(T rawValue)
        {
            RawValue = rawValue;
        }

        public bool HasRace(Race raceId)
        {
            return (RawValue & GetMaskForRace(raceId)) != 0;
        }

        public bool IsEmpty()
        {
            return RawValue == 0;
        }

        public static RaceMask<T> operator &(RaceMask<T> left, RaceMask<T> right) { return new RaceMask<T>(left.RawValue & right.RawValue); }
        public static RaceMask<T> operator |(RaceMask<T> left, RaceMask<T> right) { return new RaceMask<T>(left.RawValue | right.RawValue); }

        public static dynamic GetMaskForRace(Race raceId)
        {
            int raceBit = GetRaceBit(raceId);
            return (T)(dynamic)(raceBit >= 0 && (uint)raceBit < Marshal.SizeOf<T>() * 8 ? (1 << raceBit) : 0);
        }

        static int GetRaceBit(Race raceId)
        {
            switch (raceId)
            {
                case Race.Human:
                case Race.Orc:
                case Race.Dwarf:
                case Race.NightElf:
                case Race.Undead:
                case Race.Tauren:
                case Race.Gnome:
                case Race.Troll:
                case Race.Goblin:
                case Race.BloodElf:
                case Race.Draenei:
                case Race.Worgen:
                case Race.PandarenNeutral:
                case Race.PandarenAlliance:
                case Race.PandarenHorde:
                case Race.Nightborne:
                case Race.HighmountainTauren:
                case Race.VoidElf:
                case Race.LightforgedDraenei:
                case Race.ZandalariTroll:
                case Race.KulTiran:
                    return (int)raceId - 1;
                case Race.DarkIronDwarf:
                    return 11;
                case Race.Vulpera:
                    return 12;
                case Race.MagharOrc:
                    return 13;
                case Race.MechaGnome:
                    return 14;
                case Race.DracthyrAlliance:
                    return 16;
                case Race.DracthyrHorde:
                    return 15;
                default:
                    break;
            }
            return -1;
        }
    }
}