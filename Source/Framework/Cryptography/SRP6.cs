// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Cryptography
{
    public class SRP6
    {
        static SHA1 _sha1;
        static BigInteger _g;
        static BigInteger _N;

        static SRP6()
        {
            _sha1 = SHA1.Create();
            _g = new BigInteger(7);
            _N = new BigInteger(new byte[]
            {
                0x89, 0x4B, 0x64, 0x5E, 0x89, 0xE1, 0x53, 0x5B, 0xBD, 0xAD, 0x5B, 0x8B, 0x29, 0x06, 0x50, 0x53,
                0x08, 0x01, 0xB1, 0x8E, 0xBF, 0xBF, 0x5E, 0x8F, 0xAB, 0x3C, 0x82, 0x87, 0x2A, 0x3E, 0x9B, 0xB7,
            }, true, true);
        }

        public static (byte[] Salt, byte[] Verifier) MakeRegistrationData(string username, string password)
        {
            var salt = new byte[0].GenerateRandomKey(32); // random salt
            return (salt, CalculateVerifier(username, password, salt));
        }
        
        public static byte[] CalculateVerifier(string username, string password, byte[] salt)
        {
            // v = g ^ H(s || H(u || ':' || p)) mod N
            return BigInteger.ModPow(_g, new BigInteger(_sha1.ComputeHash(salt.Combine(_sha1.ComputeHash(Encoding.UTF8.GetBytes(username.ToUpperInvariant() + ":" + password.ToUpperInvariant())))), true), _N).ToByteArray();
        }

        public static bool CheckLogin(string username, string password, byte[] salt, byte[] verifier)
        {
            return verifier.Compare(CalculateVerifier(username, password, salt));
        }
    }
}
