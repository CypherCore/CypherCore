// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.Numerics;

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

    static double eps(float a, float b)
    {
        float aa = Math.Abs(a) + 1.0f;
        if (float.IsPositiveInfinity(aa))
            return 0.0000005f;

        return 0.0000005f * aa;
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
    public static bool fuzzyEq(float a, float b)
    {
        return (a == b) || (Math.Abs(a - b) <= eps(a, b));
    }
    public static bool fuzzyGt(float a, float b)
    {
        return a > b + eps(a, b);
    }
    public static bool fuzzyLt(float a, float b)
    {
        return a < b - eps(a, b);
    }
    public static bool fuzzyNe(float a, float b)
    {
        return !fuzzyEq(a, b);
    }
    public static bool fuzzyLe(float a, float b)
    {
        return a < b + eps(a, b);
    }
    public static bool fuzzyGe(float a, float b)
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

    public static float GetPctOf(float value, float max)
    {
        return value / max * 100.0f;
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

    public static double Variance(this IEnumerable<uint> source)
    {
        int n = 0;
        double mean = 0;
        double M2 = 0;

        foreach (var x in source)
        {
            n = n + 1;
            double delta = x - mean;
            mean = mean + delta / n;
            M2 += delta * (x - mean);
        }
        return M2 / (n - 1);
    }

    //3d math
    public static Box toWorldSpace(Matrix4x4 rotation, Vector3 translation, Box box)
    {
        if (!box.isFinite())
            return box;

        Box outBox = box;

        outBox._center = new(rotation.M11 * box._center.GetAt(0) + rotation.M12 * box._center.GetAt(1) + rotation.M13 * box._center.GetAt(2) + translation.GetAt(0),
            rotation.M21 * box._center.GetAt(0) + rotation.M22 * box._center.GetAt(1) + rotation.M23 * box._center.GetAt(2) + translation.GetAt(1),
            rotation.M31 * box._center.GetAt(0) + rotation.M32 * box._center.GetAt(1) + rotation.M33 * box._center.GetAt(2) + translation.GetAt(2));

        for (int i = 0; i < 3; ++i)
            outBox._edgeVector[i] = rotation.Multiply(box._edgeVector[i]);

        outBox._area = box._area;
        outBox._volume = box._volume;

        return box;
    }
    public static Matrix4x4 Inverse(this Matrix4x4 elt)
    {
        Matrix4x4 kInverse;
        elt.Inverse(out kInverse);
        return kInverse;
    }
    public static bool Inverse(this Matrix4x4 elt, out Matrix4x4 rkInverse)
    {
        // Invert a 3x3 using cofactors.  This is about 8 times faster than
        // the Numerical Recipes code which uses Gaussian elimination.
        rkInverse = new();
        rkInverse.M11 = elt.M22 * elt.M33 -
                          elt.M23 * elt.M32;
        rkInverse.M12 = elt.M13 * elt.M32 -
                          elt.M12 * elt.M33;
        rkInverse.M13 = elt.M12 * elt.M23 -
                          elt.M13 * elt.M22;
        rkInverse.M21 = elt.M23 * elt.M31 -
                          elt.M21 * elt.M33;
        rkInverse.M22 = elt.M11 * elt.M33 -
                          elt.M13 * elt.M31;
        rkInverse.M23 = elt.M13 * elt.M21 -
                          elt.M11 * elt.M23;
        rkInverse.M31 = elt.M21 * elt.M32 -
                          elt.M22 * elt.M31;
        rkInverse.M32 = elt.M12 * elt.M31 -
                          elt.M11 * elt.M32;
        rkInverse.M33 = elt.M11 * elt.M22 -
                          elt.M12 * elt.M21;

        float fDet =
            elt.M11 * rkInverse.M11 +
            elt.M12 * rkInverse.M21 +
            elt.M13 * rkInverse.M31;

        if (Math.Abs(fDet) <= float.Epsilon)
            return false;

        float fInvDet = 1.0f / fDet;

        rkInverse.M11 *= fInvDet;
        rkInverse.M12 *= fInvDet;
        rkInverse.M13 *= fInvDet;
        rkInverse.M21 *= fInvDet;
        rkInverse.M22 *= fInvDet;
        rkInverse.M23 *= fInvDet;
        rkInverse.M31 *= fInvDet;
        rkInverse.M32 *= fInvDet;
        rkInverse.M33 *= fInvDet;

        return true;
    }

    public static Matrix4x4 ToMatrix(this Quaternion _q)
    {
        // Implementation from Watt and Watt, pg 362
        // See also http://www.flipcode.com/documents/matrfaq.html#Q54
        Quaternion q = _q;
        q *= 1.0f / MathF.Sqrt((q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z) + (q.W * q.W));

        float xx = 2.0f * q.X * q.X;
        float xy = 2.0f * q.X * q.Y;
        float xz = 2.0f * q.X * q.Z;
        float xw = 2.0f * q.X * q.W;

        float yy = 2.0f * q.Y * q.Y;
        float yz = 2.0f * q.Y * q.Z;
        float yw = 2.0f * q.Y * q.W;

        float zz = 2.0f * q.Z * q.Z;
        float zw = 2.0f * q.Z * q.W;

        return new Matrix4x4(1.0f - yy - zz, xy - zw, xz + yw, 0.0f,
            xy + zw, 1.0f - xx - zz, yz - xw, 0.0f,
            xz - yw, yz + xw, 1.0f - xx - yy, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
    }

    public static Vector3 Multiply(this Matrix4x4 elt, Vector3 v)
    {
        return new(elt.M11 * v.GetAt(0) + elt.M12 * v.GetAt(1) + elt.M13 * v.GetAt(2),
            elt.M21 * v.GetAt(0) + elt.M22 * v.GetAt(1) + elt.M23 * v.GetAt(2),
            elt.M31 * v.GetAt(0) + elt.M32 * v.GetAt(1) + elt.M33 * v.GetAt(2));
    }
}
