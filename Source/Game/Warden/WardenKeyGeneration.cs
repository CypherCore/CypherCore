// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

namespace Game
{
	internal class SHA1Randx
	{
		private byte[] o0 = new byte[20];
		private byte[] o1 = new byte[20];
		private byte[] o2 = new byte[20];

		private SHA1 sh;
		private uint taken;

		public SHA1Randx(byte[] buff)
		{
			int        halfSize = buff.Length / 2;
			Span<byte> span     = buff;

			sh = SHA1.Create();
			o1 = sh.ComputeHash(buff, 0, halfSize);

			sh = SHA1.Create();
			o2 = sh.ComputeHash(span[halfSize..].ToArray(), 0, buff.Length - halfSize);

			FillUp();
		}

		public void Generate(byte[] buf, uint sz)
		{
			for (uint i = 0; i < sz; ++i)
			{
				if (taken == 20)
					FillUp();

				buf[i] = o0[taken];
				taken++;
			}
		}


		private void FillUp()
		{
			sh = SHA1.Create();
			sh.ComputeHash(o1, 0, 20);
			sh.ComputeHash(o0, 0, 20);
			o0 = sh.ComputeHash(o2, 0, 20);

			taken = 0;
		}
	}
}