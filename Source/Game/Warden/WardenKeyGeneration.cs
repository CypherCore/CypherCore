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

using System.Linq;
using System.Security.Cryptography;
using System;

namespace Game
{
    class SHA1Randx
    {
        public SHA1Randx(byte[] buff)
        {
            int halfSize = buff.Length / 2;
            Span<byte> span = buff;

            sh = SHA1.Create();
            o1 = sh.ComputeHash(buff, 0, halfSize);

            sh = SHA1.Create();
            o2 = sh.ComputeHash(span.Slice(halfSize).ToArray(), 0, buff.Length - halfSize);

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


        void FillUp()
        {
            sh = SHA1.Create();
            sh.ComputeHash(o1, 0, 20);
            sh.ComputeHash(o0, 0, 20);
            o0 = sh.ComputeHash(o2, 0, 20);

            taken = 0;
        }

        SHA1 sh;
        uint taken;
        byte[] o0 = new byte[20];
        byte[] o1 = new byte[20];
        byte[] o2 = new byte[20];
    }

}
