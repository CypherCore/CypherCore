// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Google.Protobuf.WellKnownTypes;
using System;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

namespace Framework.Cryptography
{
	public class HashFNV1a_64
	{
		public ulong Value { get; private set; } = FnvOffsetBasis64;

        // FNV-1a 32-bit constants
        private const uint FnvOffsetBasis32 = 2166136261;
		private const uint FnvPrime32 = 16777619;


		public static uint ComputeHash32(byte[] data)
		{
			uint hash = FnvOffsetBasis32;

			unchecked
			{
				foreach (byte b in data)
				{
					hash ^= b;      // XOR the bottom octet of the hash with the byte
					hash *= FnvPrime32; // Multiply by the FNV prime
				}
			}

			return hash;
		}

		// FNV-1a 64-bit constants
		private const ulong FnvOffsetBasis64 = 14695981039346656037UL;
		private const ulong FnvPrime64 = 1099511628211UL;

		public void ComputeHash(byte[] data)
		{
			ulong hash = Value;

			unchecked
			{
				foreach (byte b in data)
				{
					hash ^= b;
					hash *= FnvPrime64;
				}
			}

            Value = hash;
		}

        public void ComputeHash(uint data)
        {
            var bytes = BitConverter.GetBytes(data);

            ComputeHash(bytes);
        }
        public void ComputeHash(short data)
        {
            var bytes = BitConverter.GetBytes(data);

            ComputeHash(bytes);
        }
        public void ComputeHash(long data)
        {
            var bytes = BitConverter.GetBytes(data);

            ComputeHash(bytes);
        }
        public void ComputeHash(short[] data)
        {
            var bytes = new byte[data.Length * 2];
			Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);

            ComputeHash(bytes);
        }
    }
}