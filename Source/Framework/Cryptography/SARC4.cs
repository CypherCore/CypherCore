/*
 * Copyright (C) 2012-2014 Arctium Emulation<http://arctium.org>
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

namespace Framework.Cryptography
{
    //Thx Fabian over at Arctium.
    public sealed class SARC4
    {
        public SARC4()
        {
            _s = new byte[0x100];
            _tmp = 0;
            _tmp2 = 0;
        }

        public void PrepareKey(byte[] key)
        {
            for (int i = 0; i < 0x100; i++)
                _s[i] = (byte)i;

            var j = 0;
            for (int i = 0; i < 0x100; i++)
            {
                j = (byte)((j + key[i % key.Length] + _s[i]) & 255);

                var tempS = _s[i];

                _s[i] = _s[j];
                _s[j] = tempS;
            }
        }

        public void ProcessBuffer(byte[] data, int length)
        {
            for (int i = 0; i < length; i++)
            {
                _tmp = (byte)((_tmp + 1) % 0x100);
                _tmp2 = (byte)((_tmp2 + _s[_tmp]) % 0x100);

                var sTemp = _s[_tmp];

                _s[_tmp] = _s[_tmp2];
                _s[_tmp2] = sTemp;

                data[i] = (byte)(_s[(_s[_tmp] + _s[_tmp2]) % 0x100] ^ data[i]);
            }
        }

        byte[] _s;
        byte _tmp;
        byte _tmp2;
    }
}
