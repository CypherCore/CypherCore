// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace System
{
    public static class Extensions
    {
        public static bool HasAnyFlag<T>(this T value, T flag) where T : struct
        {
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flag);
            return (lValue & lFlag) != 0;
        }

        public static string ToHexString(this byte[] byteArray, bool reverse = false)
        {
            if (reverse)
                return byteArray.Reverse().Aggregate("", (current, b) => current + b.ToString("X2"));
            else
                return byteArray.Aggregate("", (current, b) => current + b.ToString("X2"));
        }
        
        public static byte[] ToByteArray(this string str)
        {
            str = str.Replace(" ", String.Empty);

            var res = new byte[str.Length / 2];
            for (int i = 0; i < res.Length; ++i)
            {
                string temp = String.Concat(str[i * 2], str[i * 2 + 1]);
                res[i] = Convert.ToByte(temp, 16);
            }
            return res;
        }

        public static byte[] ToByteArray(this string value, char separator)
        {
            return Array.ConvertAll(value.Split(separator), byte.Parse);
        }

        static uint LeftRotate(this uint value, int shiftCount)
        {
            return (value << shiftCount) | (value >> (0x20 - shiftCount));
        }

        public static byte[] GenerateRandomKey(this byte[] s, int length)
        {
            var random = new Random((int)((uint)(Guid.NewGuid().GetHashCode() ^ 1 >> 89 << 2 ^ 42)).LeftRotate(13));
            var key = new byte[length];

            for (int i = 0; i < length; i++)
            {
                int randValue;

                do
                {
                    randValue = (int)((uint)random.Next(0xFF)).LeftRotate(1) ^ i;
                } while (randValue > 0xFF && randValue <= 0);

                key[i] = (byte)randValue;
            }

            return key;
        }

        public static bool Compare(this byte[] b, byte[] b2)
        {
            for (int i = 0; i < b2.Length; i++)
                if (b[i] != b2[i])
                    return false;

            return true;
        }

        public static byte[] Combine(this byte[] data, params byte[][] pData)
        {
            var combined = data;

            foreach (var arr in pData)
            {
                var currentSize = combined.Length;

                Array.Resize(ref combined, currentSize + arr.Length);

                Buffer.BlockCopy(arr, 0, combined, currentSize, arr.Length);
            }

            return combined;
        }

        public static object[] Combine(this object[] data, params object[][] pData)
        {
            var combined = data;

            foreach (var arr in pData)
            {
                var currentSize = combined.Length;

                Array.Resize(ref combined, currentSize + arr.Length);

                Array.Copy(arr, 0, combined, currentSize, arr.Length);
            }

            return combined;
        }

        public static void Swap<T>(ref T left, ref T right)
        {
            T temp = left;
            left = right;
            right = temp;
        }

        public static uint[] SerializeObject<T>(this T obj)
        {
            //if (obj.GetType()<StructLayoutAttribute>() == null)
                //return null;

            var size = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(size);
            byte[] array = new byte[size];

            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, array, 0, size);

            Marshal.FreeHGlobal(ptr);

            uint[] result = new uint[size / 4];
            Buffer.BlockCopy(array, 0, result, 0, array.Length);

            return result;
        }

        public static List<T> DeserializeObjects<T>(this ICollection<uint> data)
        {
            List<T> list = new();

            if (data.Count == 0)
                return list;

            if (typeof(T).GetCustomAttribute<StructLayoutAttribute>() == null)
                return list;

            byte[] result = new byte[data.Count * sizeof(uint)];
            Buffer.BlockCopy(data.ToArray(), 0, result, 0, result.Length);

            var typeSize = Marshal.SizeOf(typeof(T));
            var objCount = data.Count / (typeSize / sizeof(uint));

            for (var i = 0; i < objCount; ++i)
            {
                var ptr = Marshal.AllocHGlobal(typeSize);
                Marshal.Copy(result, typeSize * i, ptr, typeSize);
                list.Add((T)Marshal.PtrToStructure(ptr, typeof(T)));
                Marshal.FreeHGlobal(ptr);
            }

            return list;
        }

        public static float GetAt(this Vector3 vector, long index)
        {
            switch (index)
            {
                case 0:
                    return vector.X;
                case 1:
                    return vector.Y;
                case 2:
                    return vector.Z;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public static void SetAt(this ref Vector3 vector, float value, long index)
        {
            switch (index)
            {
                case 0:
                    vector.X = value;
                    break;
                case 1:
                    vector.Y = value;
                    break;
                case 2:
                    vector.Z = value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public static int primaryAxis(this Vector3 vector)
        {
            var a = 0;

            double nx = Math.Abs(vector.X);
            double ny = Math.Abs(vector.Y);
            double nz = Math.Abs(vector.Z);

            if (nx > ny)
            {
                if (nx > nz)
                    a = 0;
                else
                    a = 2;
            }
            else
            {
                if (ny > nz)
                    a = 1;
                else
                    a = 2;
            }

            return a;
        }

        public static Vector3 direction(this Vector3 vector)
        {
            float lenSquared = vector.LengthSquared();
            float invSqrt = 1.0f / MathF.Sqrt(lenSquared);
            return new Vector3(vector.X * invSqrt, vector.Y * invSqrt, vector.Z * invSqrt);
        }
        
        public static Vector3 directionOrZero(this Vector3 vector)
        {
            float mag = vector.LengthSquared();
            if (mag < 0.0000001f)
            {
                return Vector3.Zero;
            }
            else if (mag < 1.00001f && mag > 0.99999f)
            {
                return vector;
            }
            else
            {
                return vector * (1.0f / mag);
            }
        }

        public static void toEulerAnglesZYX(this Quaternion quaternion, out float z, out float y, out float x)
        {
            // rot =  cy*cz           cz*sx*sy-cx*sz  cx*cz*sy+sx*sz
            //        cy*sz           cx*cz+sx*sy*sz -cz*sx+cx*sy*sz
            //       -sy              cy*sx           cx*cy

            var matrix = quaternion.ToMatrix();
            if (matrix.M31 < 1.0)
            {
                if (matrix.M31 > -1.0)
                {
                    z = MathF.Atan2(matrix.M21, matrix.M11);
                    y = MathF.Asin(-matrix.M31);
                    x = MathF.Atan2(matrix.M31, matrix.M33);
                }
                else
                {
                    // WARNING.  Not unique.  ZA - XA = -atan2(r01,r02)
                    z = -MathF.Atan2(matrix.M12, matrix.M13);
                    y = MathFunctions.PiOver2;
                    x = 0.0f;
                }
            }
            else
            {
                // WARNING.  Not unique.  ZA + XA = atan2(-r01,-r02)
                z = MathF.Atan2(-matrix.M12, -matrix.M13);
                y = -MathFunctions.PiOver2;
                x = 0.0f;
            }
        }
        
        public static Matrix4x4 fromEulerAnglesZYX(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos = MathF.Cos(fYAngle);
            float fSin = MathF.Sin(fYAngle);
            Matrix4x4 kZMat = new(fCos, -fSin, 0.0f, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);

            fCos = MathF.Cos(fPAngle);
            fSin = MathF.Sin(fPAngle);
            Matrix4x4 kYMat = new(fCos, 0.0f, fSin, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, -fSin, 0.0f, fCos, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);

            fCos = MathF.Cos(fRAngle);
            fSin = MathF.Sin(fRAngle);
            Matrix4x4 kXMat = new(1.0f, 0.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);

            return kZMat * (kYMat * kXMat);
        }

        #region Strings
        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static T ToEnum<T>(this string str) where T : struct
        {
            T value;
            if (!Enum.TryParse(str, out value))
                return default;

            return value;
        }

        public static string ConvertFormatSyntax(this string str)
        {
            string pattern = @"(%\W*\d*[a-zA-Z]*)";
            
            int count = 0;
            string result = Regex.Replace(str, pattern, m => string.Concat("{", count++, "}"));

            return result;
        }

        public static bool Like(this string toSearch, string toFind)
        {
            if (toSearch == null || toFind == null)
                return false;

            return toSearch.ToLower().Contains(toFind.ToLower());
        }

        public static bool IsNumber(this string str)
        {
            double value;
            return double.TryParse(str, out value);
        }

        public static int GetByteCount(this string str)
        {
            if (str.IsEmpty())
                return 0;

            return Encoding.UTF8.GetByteCount(str);
        }

        public static bool isExtendedLatinCharacter(char wchar)
        {
            if (isBasicLatinCharacter(wchar))
                return true;
            if (wchar >= 0x00C0 && wchar <= 0x00D6)                  // LATIN CAPITAL LETTER A WITH GRAVE - LATIN CAPITAL LETTER O WITH DIAERESIS
                return true;
            if (wchar >= 0x00D8 && wchar <= 0x00DE)                  // LATIN CAPITAL LETTER O WITH STROKE - LATIN CAPITAL LETTER THORN
                return true;
            if (wchar == 0x00DF)                                     // LATIN SMALL LETTER SHARP S
                return true;
            if (wchar >= 0x00E0 && wchar <= 0x00F6)                  // LATIN SMALL LETTER A WITH GRAVE - LATIN SMALL LETTER O WITH DIAERESIS
                return true;
            if (wchar >= 0x00F8 && wchar <= 0x00FE)                  // LATIN SMALL LETTER O WITH STROKE - LATIN SMALL LETTER THORN
                return true;
            if (wchar >= 0x0100 && wchar <= 0x012F)                  // LATIN CAPITAL LETTER A WITH MACRON - LATIN SMALL LETTER I WITH OGONEK
                return true;
            if (wchar == 0x1E9E)                                     // LATIN CAPITAL LETTER SHARP S
                return true;
            return false;
        }

        public static bool isBasicLatinCharacter(char wchar)
        {
            if (wchar >= 'a' && wchar <= 'z')                      // LATIN SMALL LETTER A - LATIN SMALL LETTER Z
                return true;
            if (wchar >= 'A' && wchar <= 'Z')                      // LATIN CAPITAL LETTER A - LATIN CAPITAL LETTER Z
                return true;
            return false;
        }

        public static Vector3 ParseVector3(this string value)
        {
            Regex r = new Regex(@"\((?<x>.*),(?<y>.*),(?<z>.*)\)", RegexOptions.Singleline);
            Match m = r.Match(value);
            if (m.Success)
            {
                return new Vector3(
                    float.Parse(m.Result("${x}")),
                    float.Parse(m.Result("${y}")),
                    float.Parse(m.Result("${z}"))
                    );
            }
            else
            {
                throw new Exception("Unsuccessful Match.");
            }
        }

        public static (string token, string tail) Tokenize(this string args)
        {
            string token;
            string tail = "";

            int delimPos = args.IndexOf(' ');
            if (delimPos != -1)
            {
                token = args.Substring(0, delimPos);
                int tailPos = args.FindFirstNotOf(" ", delimPos);
                if (tailPos != -1)
                    tail = args.Substring(tailPos);
            }
            else
                token = args;

            return (token, tail);
        }

        public static int FindFirstNotOf(this string source, string chars, int pos)
        {
            for (int i = pos; i < source.Length; i++)
                if (chars.IndexOf(source[i]) == -1)
                    return i;

            return -1;
        }
        #endregion

        #region BinaryReader
        public static string ReadCString(this BinaryReader reader)
        {
            byte num;
            List<byte> temp = new();

            while ((num = reader.ReadByte()) != 0)
                temp.Add(num);

            return Encoding.UTF8.GetString(temp.ToArray());
        }

        public static string ReadString(this BinaryReader reader, int count)
        {
            var array = reader.ReadBytes(count);
            return Encoding.ASCII.GetString(array);
        }

        public static string ReadStringFromChars(this BinaryReader reader, int count)
        {
            return new string(reader.ReadChars(count));
        }

        public static T[] ReadArray<T>(this BinaryReader reader, uint size) where T : struct
        {
            int numBytes = Unsafe.SizeOf<T>() * (int)size;

            byte[] source = reader.ReadBytes(numBytes);

            T[] result = new T[source.Length / Unsafe.SizeOf<T>()];

            if (source.Length > 0)
            {
                unsafe
                {
                    Unsafe.CopyBlockUnaligned(Unsafe.AsPointer(ref result[0]), Unsafe.AsPointer(ref source[0]), (uint)source.Length);
                }
            }

            return result;
        }

        public static T Read<T>(this BinaryReader reader) where T : struct
        {
            byte[] result = reader.ReadBytes(Unsafe.SizeOf<T>());

            return Unsafe.ReadUnaligned<T>(ref result[0]);
        }
        #endregion
    }
}