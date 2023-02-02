// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
