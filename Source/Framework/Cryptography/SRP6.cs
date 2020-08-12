using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Numerics;

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
            _g = new byte[] { 7 }.ToBigInteger();
            _N = new byte[]
            {
                0x89, 0x4B, 0x64, 0x5E, 0x89, 0xE1, 0x53, 0x5B, 0xBD, 0xAD, 0x5B, 0x8B, 0x29, 0x06, 0x50, 0x53,
                0x08, 0x01, 0xB1, 0x8E, 0xBF, 0xBF, 0x5E, 0x8F, 0xAB, 0x3C, 0x82, 0x87, 0x2A, 0x3E, 0x9B, 0xB7,
            }.ToBigInteger(true);
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
            return CalculateVerifierFromHash(_sha1.ComputeHash(Encoding.ASCII.GetBytes(username.ToUpper() + ":" + password.ToUpper())), salt);
        }

        // merge this into CalculateVerifier once the sha_pass hack finally gets nuked from orbit
        public static byte[] CalculateVerifierFromHash(byte[] hash, byte[] salt)
        {
            //            v = BigInteger.ModPow(gBN, x, BN);
            return BigInteger.ModPow(_g, _sha1.ComputeHash(salt.Combine(hash)).ToBigInteger(), _N).ToByteArray();
        }

        public static bool CheckLogin(string username, string password, byte[] salt, byte[] verifier)
        {
            return verifier.Compare(CalculateVerifier(username, password, salt));
        }
    }
}
