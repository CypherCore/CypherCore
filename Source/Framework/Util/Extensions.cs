﻿/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string ToHexString(this byte[] byteArray)
        {
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
            return Array.ConvertAll(value.Split(separator), s => byte.Parse(s));
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
                int randValue = -1;

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

        public static BigInteger ToBigInteger<T>(this T value, bool isBigEndian = false)
        {
            var ret = BigInteger.Zero;

            switch (typeof(T).Name)
            {
                case "Byte[]":
                    var data = value as byte[];

                    if (isBigEndian)
                        Array.Reverse(data);

                    ret = new BigInteger(data.Combine(new byte[] { 0 }));
                    break;
                case "BigInteger":
                    ret = (BigInteger)Convert.ChangeType(value, typeof(BigInteger));
                    break;
                default:
                    throw new NotSupportedException(string.Format("'{0}' conversion to 'BigInteger' not supported.", typeof(T).Name));
            }

            return ret;
        }

        public static void Swap<T>(ref T left, ref T right)
        {
            T temp = left;
            left = right;
            right = temp;
        }

        public static Func<object, object> CompileGetter(this FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(object), new[] { typeof(object) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldsfld, field);
                gen.Emit(field.FieldType.IsClass ? OpCodes.Castclass : OpCodes.Box, field.FieldType);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Castclass, field.DeclaringType);
                gen.Emit(OpCodes.Ldfld, field);
                gen.Emit(field.FieldType.IsClass ? OpCodes.Castclass : OpCodes.Box, field.FieldType);
            }
            gen.Emit(OpCodes.Ret);
            return (Func<object, object>)setterMethod.CreateDelegate(typeof(Func<object, object>));
        }

        public static Action<object, object> CompileSetter(this FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new[] { typeof(object), typeof(object) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(field.FieldType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, field.FieldType);
                gen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Castclass, field.DeclaringType);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(field.FieldType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, field.FieldType);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Action<object, object>)setterMethod.CreateDelegate(typeof(Action<object, object>));
        }

        public static Func<T, object> GetGetter<T>(this FieldInfo fieldInfo)
        {
            var paramExpression = Expression.Parameter(typeof(T));
            var propertyExpression = Expression.Field(paramExpression, fieldInfo);
            var convertExpression = Expression.TypeAs(propertyExpression, typeof(object));

            return Expression.Lambda<Func<T, object>>(convertExpression, paramExpression).Compile();
        }

        public static Action<T, object> GetSetter<T>(this FieldInfo fieldInfo)
        {
            var paramExpression = Expression.Parameter(typeof(T));
            var propertyExpression = Expression.Field(paramExpression, fieldInfo);
            var valueExpression = Expression.Parameter(typeof(object));
            var convertExpression = Expression.Convert(valueExpression, fieldInfo.FieldType);
            var assignExpression = Expression.Assign(propertyExpression, convertExpression);

            return Expression.Lambda<Action<T, object>>(assignExpression, paramExpression, valueExpression).Compile();
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
            List<T> list = new List<T>();

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

        #region Strings
        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static T ToEnum<T>(this string str) where T : struct
        {
            T value;
            if (!Enum.TryParse(str, out value))
                return default(T);

            return value;
        }

        public static string ConvertFormatSyntax(this string str)
        {
            string pattern = @"(%\W*\d*[a-zA-Z]*)";
            
            int count = 0;
            string result = Regex.Replace(str, pattern, m =>
            {
                return string.Concat("{", count++, "}");
            });

            return result;
        }

        public static bool Like(this string toSearch, string toFind)
        {
            return toSearch.ToLower().Contains(toFind.ToLower());
        }

        public static bool IsNumber(this string str)
        {
            double value;
            return double.TryParse(str, out value);
        }

        public static int GetByteCount(this string str)
        {
            return Encoding.UTF8.GetByteCount(str);
        }
        #endregion

        #region BinaryReader
        public static T ReadStruct<T>(this BinaryReader reader) where T : struct
        {
            byte[] data = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            T returnObject = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();
            return returnObject;
        }

        public static T ReadStruct<T>(this BinaryReader reader, uint offset) where T : struct
        {
            reader.BaseStream.Position = offset;
            byte[] data = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            T returnObject = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();
            return returnObject;
        }

        public static string ReadCString(this BinaryReader reader)
        {
            byte num;
            List<byte> temp = new List<byte>();

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

        public static byte[] ToByteArray(this BinaryReader reader)
        {
            var data = new byte[reader.BaseStream.Length];

            long pos = reader.BaseStream.Position;
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)reader.BaseStream.ReadByte();

            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            return data;

        }
        #endregion
    }
}