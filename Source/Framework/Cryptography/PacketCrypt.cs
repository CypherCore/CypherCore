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

using System;
using System.Security.Cryptography;

namespace Framework.Cryptography
{
    public sealed class WorldCrypt : IDisposable
    {
        public void Initialize(byte[] key)
        {
            if (IsInitialized)
                throw new InvalidOperationException("PacketCrypt already initialized!");

            _serverEncrypt = new AesGcm(key);
            _clientDecrypt = new AesGcm(key);

            IsInitialized = true;
        }

        public bool Encrypt(ref byte[] data, ref byte[] tag)
        {
            try
            {
                if (IsInitialized)
                    _serverEncrypt.Encrypt(BitConverter.GetBytes(_serverCounter).Combine(BitConverter.GetBytes(0x52565253)), data, data, tag);

                ++_serverCounter;
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public bool Decrypt(byte[] data, byte[] tag)
        {
            try
            {
                if (IsInitialized)
                    _clientDecrypt.Decrypt(BitConverter.GetBytes(_clientCounter).Combine(BitConverter.GetBytes(0x544E4C43)), data, tag, data);

                ++_clientCounter;
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            IsInitialized = false;
        }

        public bool IsInitialized { get; set; }

        AesGcm _serverEncrypt;
        AesGcm _clientDecrypt;
        ulong _clientCounter;
        ulong _serverCounter;
    }
}
