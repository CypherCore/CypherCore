// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Game.Miscellaneous
{
    public struct RaceMask
    {
        public static RaceMask<T> All_V<T>(int size = 1) where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T> => ~new RaceMask<T>(size);

        public static RaceMask<T> AllPlayable_V<T>(int size = 1) where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T> =>
            RaceMask<T>.GetMaskForRace(Race.Human, size) | RaceMask<T>.GetMaskForRace(Race.Orc, size) | RaceMask<T>.GetMaskForRace(Race.Dwarf, size) | RaceMask<T>.GetMaskForRace(Race.NightElf, size) |
            RaceMask<T>.GetMaskForRace(Race.Undead, size) | RaceMask<T>.GetMaskForRace(Race.Tauren, size) | RaceMask<T>.GetMaskForRace(Race.Gnome, size) | RaceMask<T>.GetMaskForRace(Race.Troll, size) |
            RaceMask<T>.GetMaskForRace(Race.BloodElf, size) | RaceMask<T>.GetMaskForRace(Race.Draenei, size) | RaceMask<T>.GetMaskForRace(Race.Goblin, size) | RaceMask<T>.GetMaskForRace(Race.Worgen, size) |
            RaceMask<T>.GetMaskForRace(Race.PandarenNeutral, size) | RaceMask<T>.GetMaskForRace(Race.PandarenAlliance, size) | RaceMask<T>.GetMaskForRace(Race.PandarenHorde, size) | RaceMask<T>.GetMaskForRace(Race.Nightborne, size) |
            RaceMask<T>.GetMaskForRace(Race.HighmountainTauren, size) | RaceMask<T>.GetMaskForRace(Race.VoidElf, size) | RaceMask<T>.GetMaskForRace(Race.LightforgedDraenei, size) | RaceMask<T>.GetMaskForRace(Race.ZandalariTroll, size) |
            RaceMask<T>.GetMaskForRace(Race.KulTiran, size) | RaceMask<T>.GetMaskForRace(Race.DarkIronDwarf, size) | RaceMask<T>.GetMaskForRace(Race.Vulpera, size) | RaceMask<T>.GetMaskForRace(Race.MagharOrc, size) |
            RaceMask<T>.GetMaskForRace(Race.MechaGnome, size) | RaceMask<T>.GetMaskForRace(Race.DracthyrAlliance, size) | RaceMask<T>.GetMaskForRace(Race.DracthyrHorde, size) | RaceMask<T>.GetMaskForRace(Race.EarthenDwarfHorde, size) |
            RaceMask<T>.GetMaskForRace(Race.EarthenDwarfAlliance, size) | RaceMask<T>.GetMaskForRace(Race.HaranirAlliance, size) | RaceMask<T>.GetMaskForRace(Race.HaranirHorde, size);

        public static RaceMask<T> Neutral_V<T>(int size = 1) where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T> => RaceMask<T>.GetMaskForRace(Race.PandarenNeutral, size);

        public static RaceMask<T> Alliance_V<T>(int size = 1) where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T> =>
           RaceMask<T>.GetMaskForRace(Race.Human, size) | RaceMask<T>.GetMaskForRace(Race.Dwarf, size) | RaceMask<T>.GetMaskForRace(Race.NightElf, size) |
           RaceMask<T>.GetMaskForRace(Race.Gnome, size) | RaceMask<T>.GetMaskForRace(Race.Draenei, size) | RaceMask<T>.GetMaskForRace(Race.Worgen, size) |
           RaceMask<T>.GetMaskForRace(Race.PandarenAlliance, size) | RaceMask<T>.GetMaskForRace(Race.VoidElf, size) | RaceMask<T>.GetMaskForRace(Race.LightforgedDraenei, size) |
           RaceMask<T>.GetMaskForRace(Race.KulTiran, size) | RaceMask<T>.GetMaskForRace(Race.DarkIronDwarf, size) | RaceMask<T>.GetMaskForRace(Race.MechaGnome, size) | RaceMask<T>.GetMaskForRace(Race.DracthyrAlliance, size) |
           RaceMask<T>.GetMaskForRace(Race.EarthenDwarfAlliance, size) | RaceMask<T>.GetMaskForRace(Race.HaranirAlliance, size);

        public static RaceMask<T> Horde_V<T>(int size = 1) where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T> => AllPlayable_V<T>(size) & ~(Neutral_V<T>(size) | Alliance_V<T>(size));

        public static RaceMask<ulong> All = All_V<ulong>();
        public static RaceMask<ulong> AllPlayable = AllPlayable_V<ulong>();
        public static RaceMask<ulong> Neutral = Neutral_V<ulong>();
        public static RaceMask<ulong> Alliance = Alliance_V<ulong>();
        public static RaceMask<ulong> Horde = Horde_V<ulong>();
    }

    public class RaceMask<T> where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T>
    {
        public T[] RawValue;
        public int Size;

        public RaceMask(T[] rawValue)
        {
            RawValue = rawValue;
            Size = rawValue.Length;
        }

        public RaceMask(int size = 1)
        {
            Size = size;
            RawValue = new T[size];
        }

        public bool HasRace(Race raceId)
        {
            int raceBit = GetRaceBit(raceId);
            return raceBit >= 0 && raceBit < Marshal.SizeOf<T>() * 8 * RawValue.Length
                && (RawValue[raceBit / (Marshal.SizeOf<T>() * 8)] & T.One << (raceBit % (Marshal.SizeOf<T>() * 8))) != T.Zero;
        }

        public static RaceMask<T> GetMaskForRace(Race raceId, int size)
        {
            var g = Marshal.SizeOf<T>();


            RaceMask<T> result = new(size);
            int raceBit = GetRaceBit(raceId);
            if (raceBit >= 0 && raceBit < Marshal.SizeOf<T>() * 8 * size)
                result.RawValue[raceBit / (Marshal.SizeOf<T>() * 8)] = T.One << (raceBit % (Marshal.SizeOf<T>() * 8));
            return result;
        }

        public bool IsEmpty()
        {
            foreach (T rawValue in RawValue)
                if (rawValue != T.Zero)
                    return false;
            return true;
        }

        public static RaceMask<T> operator &(RaceMask<T> left, RaceMask<T> right)
        {
            RaceMask<T> result = new RaceMask<T>(left.Size);
            for (int i = 0; i < left.Size; ++i)
                result.RawValue[i] = left.RawValue[i] & right.RawValue[i];
            return result;
        }
        public static RaceMask<T> operator |(RaceMask<T> left, RaceMask<T> right)
        {
            RaceMask<T> result = new(left.Size);
            for (int i = 0; i < left.Size; ++i)
                result.RawValue[i] = left.RawValue[i] | right.RawValue[i];
            return result;
        }
        public static RaceMask<T> operator ~(RaceMask<T> value)
        {
            var result = new RaceMask<T>(value.Size);
            for (int i = 0; i < value.Size; ++i)
                result.RawValue[i] = ~value.RawValue[i];
            return result;
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
                case Race.EarthenDwarfHorde:
                    return 17;
                case Race.EarthenDwarfAlliance:
                    return 18;
                case Race.HaranirHorde:
                    return 19;
                case Race.HaranirAlliance:
                    return 20;
                default:
                    break;
            }
            return -1;
        }
    }
}