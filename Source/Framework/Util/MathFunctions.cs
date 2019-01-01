/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using System;

public static class MathFunctions
{
    public const float E = 2.71828f;
    public const float Log10E = 0.434294f;
    public const float Log2E = 1.4427f;
    public const float PI = 3.14159f;
    public const float PiOver2 = 1.5708f;
    public const float PiOver4 = 0.785398f;
    public const float TwoPi = 6.28319f;
    public const float Epsilon = 4.76837158203125E-7f;

    public static float wrap(float t, float lo, float hi)
    {
        if ((t >= lo) && (t < hi))
        {
            return t;
        }

        float interval = hi - lo;
        return (float)(t - interval * Math.Floor((t - lo) / interval));
    }

    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp = lhs;
        lhs = rhs;
        rhs = temp;
    }

    #region Clamp
    /// <summary>
    /// Clamp a <paramref name="value"/> to <paramref name="calmpedValue"/> if it is withon the <paramref name="tolerance"/> range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="calmpedValue">The clamped value.</param>
    /// <param name="tolerance">The tolerance value.</param>
    /// <returns>
    /// Returns the clamped value.
    /// result = (tolerance > Abs(value-calmpedValue)) ? calmpedValue : value;
    /// </returns>
    public static float Clamp(float value, float calmpedValue, float tolerance)
    {
        return (tolerance > Math.Abs(value - calmpedValue)) ? calmpedValue : value;
    }
    /// <summary>
    /// Clamp a <paramref name="value"/> to <paramref name="calmpedValue"/> using the default tolerance value.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="calmpedValue">The clamped value.</param>
    /// <returns>
    /// Returns the clamped value.
    /// result = (EpsilonF > Abs(value-calmpedValue)) ? calmpedValue : value;
    /// </returns>
    /// <remarks><see cref="MathFunctions.Epsilon"/> is used for tolerance.</remarks>
    public static float Clamp(float value, float calmpedValue)
    {
        return (Epsilon > Math.Abs(value - calmpedValue)) ? calmpedValue : value;
    }
    #endregion

    static double eps(double a, double b)
    {
        double aa = Math.Abs(a) + 1.0;
        if (double.IsPositiveInfinity(aa))
            return double.Epsilon;

        return double.Epsilon * aa;
    }

    public static float lerp(float a, float b, float f)
    {
        return a + (b - a) * f;
    }

    public static float DegToRad(float degrees)
    {
        return degrees * (2.0f * PI / 360.0f);
    }

    #region Fuzzy
    public static bool fuzzyEq(double a, double b)
    {
        return (a == b) || (Math.Abs(a - b) <= eps(a, b));
    }
    public static bool fuzzyGt(double a, double b)
    {
        return a > b + eps(a, b);
    }
    public static bool fuzzyLt(double a, double b)
    {
        return a < b - eps(a, b);
    }
    public static bool fuzzyNe(double a, double b)
    {
        return !fuzzyEq(a, b);
    }
    public static bool fuzzyLe(double a, double b)
    {
        return a < b + eps(a, b);
    }
    public static bool fuzzyGe(double a, double b)
    {
        return a > b - eps(a, b);
    }
    #endregion

    public static int ApplyPct(ref int Base, float pct)
    {
        return Base = CalculatePct(Base, pct);
    }
    public static uint ApplyPct(ref uint Base, float pct)
    {
        return Base = CalculatePct(Base, pct);
    }
    public static float ApplyPct(ref float Base, float pct)
    {
        return Base = CalculatePct(Base, pct);
    }

    public static long AddPct(ref long value, float pct)
    {
        return value += (long)CalculatePct(value, pct);
    }
    public static int AddPct(ref int value, float pct)
    {
        return value += CalculatePct(value, pct);
    }
    public static uint AddPct(ref uint value, float pct)
    {
        return value += CalculatePct(value, pct);
    }
    public static float AddPct(ref float value, float pct)
    {
        return value += CalculatePct(value, pct);
    }

    public static int CalculatePct(int value, float pct)
    {
        return (int)(value * Convert.ToSingle(pct) / 100.0f);
    }
    public static uint CalculatePct(uint value, float pct)
    {
        return (uint)(value * Convert.ToSingle(pct) / 100.0f);
    }
    public static float CalculatePct(float value, float pct)
    {
        return value * pct / 100.0f;
    }
    public static ulong CalculatePct(ulong value, float pct)
    {
        return (ulong)(value * pct / 100.0f);
    }

    public static int RoundToInterval(ref int num, dynamic floor, dynamic ceil)
    {
        return num = (int)Math.Min(Math.Max(num, floor), ceil);
    }
    public static uint RoundToInterval(ref uint num, dynamic floor, dynamic ceil)
    {
        return num = Math.Min(Math.Max(num, floor), ceil);
    }
    public static float RoundToInterval(ref float num, dynamic floor, dynamic ceil)
    {
        return num = Math.Min(Math.Max(num, floor), ceil);
    }

    public static void ApplyPercentModFloatVar(ref float value, float val, bool apply)
    {
        if (val == -100.0f)     // prevent set var to zero
            val = -99.99f;
        value *= (apply ? (100.0f + val) / 100.0f : 100.0f / (100.0f + val));
    }

    public static bool CompareValues(ComparisionType type, uint val1, uint val2)
    {
        switch (type)
        {
            case ComparisionType.EQ:
                return val1 == val2;
            case ComparisionType.High:
                return val1 > val2;
            case ComparisionType.Low:
                return val1 < val2;
            case ComparisionType.HighEQ:
                return val1 >= val2;
            case ComparisionType.LowEQ:
                return val1 <= val2;
            default:
                // incorrect parameter
                Cypher.Assert(false);
                return false;
        }

    }
    public static bool CompareValues(ComparisionType type, float val1, float val2)
    {
        switch (type)
        {
            case ComparisionType.EQ:
                return val1 == val2;
            case ComparisionType.High:
                return val1 > val2;
            case ComparisionType.Low:
                return val1 < val2;
            case ComparisionType.HighEQ:
                return val1 >= val2;
            case ComparisionType.LowEQ:
                return val1 <= val2;
            default:
                // incorrect parameter
                Cypher.Assert(false);
                return false;
        }
    }

    public static ulong MakePair64(uint l, uint h)
    {
        return (ulong)l | ((ulong)h << 32);
    }
    public static uint Pair64_HiPart(ulong x)
    {
        return (uint)((x >> 32) & 0x00000000FFFFFFFF);
    }
    public static uint Pair64_LoPart(ulong x)
    {
        return (uint)(x & 0x00000000FFFFFFFF);
    }
    public static ushort Pair32_HiPart(uint x)
    {
        return (ushort)((x >> 16) & 0x0000FFFF);
    }
    public static ushort Pair32_LoPart(uint x)
    {
        return (ushort)(x & 0x0000FFFF);
    }
    public static uint MakePair32(uint l, uint h)
    {
        return (ushort)l | (h << 16);
    }
    public static ushort MakePair16(uint l, uint h)
    {
        return (ushort)((byte)l | (ushort)h << 8);
    }
}
