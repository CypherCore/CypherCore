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
            _sha1 = new SHA1Managed();
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

        [Obsolete]
        public static (byte[] Salt, byte[] Verifier) MakeRegistrationDataFromHash(byte[] hash)
        {
            var salt = new byte[0].GenerateRandomKey(32); // random salt
            return (salt, CalculateVerifierFromHash(hash, salt));
        }
        
        public static byte[] CalculateVerifier(string username, string password, byte[] salt)
        {
            // v = g ^ H(s || H(u || ':' || p)) mod N
            return CalculateVerifierFromHash(_sha1.ComputeHash(Encoding.UTF8.GetBytes(username.ToUpperInvariant() + ":" + password.ToUpperInvariant())), salt);
        }

        // merge this into CalculateVerifier once the sha_pass hack finally gets nuked from orbit
        public static byte[] CalculateVerifierFromHash(byte[] hash, byte[] salt)
        {
            //            v = BigInteger.ModPow(gBN, x, BN);
            return BigInteger.ModPow(_g, new BigInteger(_sha1.ComputeHash(salt.Combine(hash)), true), _N).ToByteArray();
        }

        public static bool CheckLogin(string username, string password, byte[] salt, byte[] verifier)
        {
            return verifier.Compare(CalculateVerifier(username, password, salt));
        }
    }
}
