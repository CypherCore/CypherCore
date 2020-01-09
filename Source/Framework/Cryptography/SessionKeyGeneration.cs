/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using System.Security.Cryptography;

namespace Framework.Cryptography
{
    public class SessionKeyGenerator
    {
        public SessionKeyGenerator(byte[] buff, int size)
        {
            int halfSize = size / 2;

            sh = SHA256.Create();
            sh.TransformFinalBlock(buff, 0, halfSize);
            o1 = sh.Hash;

            sh.Initialize();
            sh.TransformFinalBlock(buff, halfSize, size - halfSize);
            o2 = sh.Hash;

            FillUp();
        }

        public void Generate(byte[] buf, uint sz)
        {
            for (uint i = 0; i < sz; ++i)
            {
                if (taken == 32)
                    FillUp();

                buf[i] = o0[taken];
                taken++;
            }
        }

        void FillUp()
        {
            sh.Initialize();
            sh.TransformBlock(o1, 0, 32, o1, 0);
            sh.TransformBlock(o0, 0, 32, o0, 0);
            sh.TransformFinalBlock(o2, 0, 32);
            o0 = sh.Hash;

            taken = 0;
        }

        SHA256 sh;
        uint taken;
        byte[] o0 = new byte[32];
        byte[] o1 = new byte[32];
        byte[] o2 = new byte[32];
    }
}
