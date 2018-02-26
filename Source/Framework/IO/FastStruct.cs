/*
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Framework.IO
{
    public static class FastStruct<T> where T : struct
    {
        private delegate T LoadFromByteRefDelegate(ref byte source);
        private delegate void CopyMemoryDelegate(ref T dest, ref byte src, int count);

        private readonly static LoadFromByteRefDelegate LoadFromByteRef = BuildLoadFromByteRefMethod();
        private readonly static CopyMemoryDelegate CopyMemory = BuildCopyMemoryMethod();

        public static readonly int Size = Marshal.SizeOf<T>();

        private static LoadFromByteRefDelegate BuildLoadFromByteRefMethod()
        {
            var methodLoadFromByteRef = new DynamicMethod("LoadFromByteRef<" + typeof(T).FullName + ">",
                typeof(T), new[] { typeof(byte).MakeByRefType() }, typeof(FastStruct<T>));

            ILGenerator generator = methodLoadFromByteRef.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldobj, typeof(T));
            generator.Emit(OpCodes.Ret);

            return (LoadFromByteRefDelegate)methodLoadFromByteRef.CreateDelegate(typeof(LoadFromByteRefDelegate));
        }

        private static CopyMemoryDelegate BuildCopyMemoryMethod()
        {
            var methodCopyMemory = new DynamicMethod("CopyMemory<" + typeof(T).FullName + ">",
                typeof(void), new[] { typeof(T).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) }, typeof(FastStruct<T>));

            ILGenerator generator = methodCopyMemory.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Cpblk);
            generator.Emit(OpCodes.Ret);

            return (CopyMemoryDelegate)methodCopyMemory.CreateDelegate(typeof(CopyMemoryDelegate));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ArrayToStructure(byte[] src)
        {
            return LoadFromByteRef(ref src[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ArrayToStructure(byte[] src, int offset)
        {
            return LoadFromByteRef(ref src[offset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ArrayToStructure(ref byte src)
        {
            return LoadFromByteRef(ref src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ReadArray(byte[] source)
        {
            T[] buffer = new T[source.Length / Size];

            if (source.Length > 0)
                CopyMemory(ref buffer[0], ref source[0], source.Length);

            return buffer;
        }
    }
}
