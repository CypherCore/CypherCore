// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Cryptography
{
    public abstract class SRP6
    {
        public static int SaltLength = 32;

        public byte[] s = new byte[SaltLength]; // s - the user's password salt, random, used to calculate v on registration

        protected BigInteger I; // H(I) - the username, all uppercase
        protected BigInteger b; // b - randomly chosen by the server, same length as N, never given out
        protected BigInteger v; // v - the user's password verifier, derived from s + H(USERNAME || ":" || PASSWORD)

        public BigInteger B; // B = k*v + g^b

        bool _used; // a single instance can only be used to verify once

        public SRP6()
        {
            s = new byte[0].GenerateRandomKey(SaltLength);
            _used = true;
        }

        public SRP6(BigInteger i, byte[] salt, byte[] verifier, BigInteger N, BigInteger g, BigInteger k)
        {
            s = salt;
            I = i;
            b = CalculatePrivateB(N);
            v = new BigInteger(verifier, true);
            B = CalculatePublicB(N, g, k);
        }

        public BigInteger? VerifyClientEvidence(BigInteger A, BigInteger clientM1)
        {
            Cypher.Assert(!_used, "A single SRP6 object must only ever be used to verify ONCE!");
            _used = true;

            return DoVerifyClientEvidence(A, clientM1);
        }

        public bool CheckCredentials(string username, string password)
        {
            return v == new BigInteger(CalculateVerifier(username, password, s), true);
        }

        BigInteger CalculatePrivateB(BigInteger N)
        {
            BigInteger b = new BigInteger(new byte[0].GenerateRandomKey((int)N.GetBitLength()), true);
            b %= (N - 1);
            return b;
        }

        BigInteger CalculatePublicB(BigInteger N, BigInteger g, BigInteger k)
        {
            return (BigInteger.ModPow(g, b, N) + (v * k)) % N;
        }

        byte[] CalculateVerifier(string username, string password, byte[] salt)
        {
            // v = g ^ H(s || H(u || ':' || p)) mod N
            return BigInteger.ModPow(Getg(), CalculateX(username, password, salt), GetN()).ToByteArray();
        }

        public abstract BigInteger GetN();
        public abstract BigInteger Getg();

        public abstract BigInteger CalculateServerEvidence(BigInteger A, BigInteger clientM1, BigInteger K);

        public static (byte[], byte[]) MakeAccountRegistrationData<T>(string username, string password) where T : new()
        {
            GruntSRP6 impl = new();
            return (impl.s, impl.CalculateVerifier(username, password, impl.s));
        }

        public static (byte[], byte[]) MakeBNetRegistrationData<T>(string username, string password) where T : new()
        {
            BnetSRP6v2Hash256 impl = new();
            return (impl.s, impl.CalculateVerifier(username, password, impl.s));
        }

        public abstract BigInteger CalculateX(string username, string password, byte[] salt);


        public abstract BigInteger? DoVerifyClientEvidence(BigInteger A, BigInteger clientM1);
    }

    public class GruntSRP6 : SRP6
    {
        static BigInteger N;// the modulus, an algorithm parameter; all operations are mod this
        static BigInteger g;// a [g]enerator for the ring of integers mod N, algorithm parameter

        static SHA1 _sha1 = SHA1.Create();

        static GruntSRP6()
        {
            N = new BigInteger("894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7".ToByteArray(), true, true);
            g = new BigInteger(7u);
        }

        public GruntSRP6() : base() { }
        public GruntSRP6(string username, byte[] salt, byte[] verifier) : base(new BigInteger(_sha1.ComputeHash(Encoding.UTF8.GetBytes(username)), true, true), salt, verifier, N, g, 3) { }

        public override BigInteger CalculateServerEvidence(BigInteger A, BigInteger clientM1, BigInteger K)
        {
            return new BigInteger(_sha1.ComputeHash(A.ToByteArray().Combine(clientM1.ToByteArray().Combine(K.ToByteArray()))), true, true);
        }

        public override BigInteger CalculateX(string username, string password, byte[] salt)
        {
            return new BigInteger(_sha1.ComputeHash(salt.Combine(_sha1.ComputeHash(Encoding.UTF8.GetBytes(username + ":" + password)))), true, true);
        }

        public override BigInteger? DoVerifyClientEvidence(BigInteger A, BigInteger clientM1)
        {
            if ((A % N).IsZero)
                return null;

            BigInteger u = new(_sha1.ComputeHash(A.ToByteArray().Combine(B.ToByteArray())), false, true);
            byte[] S = BigInteger.ModPow(A * BigInteger.ModPow(v, u, N), b, N).ToByteArray();

            byte[] K = SHA1Interleave(S);

            // NgHash = H(N) xor H(g)
            byte[] NHash = _sha1.ComputeHash(N.ToByteArray());
            byte[] gHash = _sha1.ComputeHash(g.ToByteArray());
            byte[] NgHash = NHash.Select((x, i) => (byte)(x ^ gHash[i])).ToArray();

            BigInteger ourM = new BigInteger(_sha1.ComputeHash(NgHash.Combine(I.ToByteArray().Combine(s.Combine(A.ToByteArray().Combine(B.ToByteArray().Combine(K)))))), true, true);
            if (ourM == clientM1)
                return new BigInteger(K, true, true);

            return null;
        }

        byte[] SHA1Interleave(byte[] S)
        {
            // split S into two buffers
            byte[] buf0 = new byte[32 / 2];
            byte[] buf1 = new byte[32 / 2];
            for (int i = 0; i < S.Length / 2; ++i)
            {
                buf0[i] = S[2 * i + 0];
                buf1[i] = S[2 * i + 1];
            }

            // find position of first nonzero byte
            int p = 0;
            while (p < S.Length && S[p] == 0)
                ++p;

            if ((p & 1) != 0)
                ++p; // skip one extra byte if p is odd
            p /= 2; // offset into buffers

            // hash each of the halves, starting at the first nonzero byte
            byte[] hash0 = _sha1.ComputeHash(buf0[p..].Combine(S[(S.Length / 2 - p)..]));
            byte[] hash1 = _sha1.ComputeHash(buf1[p..].Combine(S[(S.Length / 2 - p)..]));

            // stick the two hashes back together
            byte[] K = new byte[SHA1.HashSizeInBytes * 2];
            for (int i = 0; i < SHA1.HashSizeInBytes; ++i)
            {
                K[2 * i + 0] = hash0[i];
                K[2 * i + 1] = hash1[i];
            }
            return K;
        }

        public override BigInteger GetN() { return N; }

        public override BigInteger Getg() { return g; }
    }

    public abstract class BnetSRP6Base : SRP6
    {
        protected static SHA256 _sha256 = SHA256.Create();
        protected static SHA512 _sha512 = SHA512.Create();

        public BnetSRP6Base() : base() { }
        public BnetSRP6Base(BigInteger i, byte[] salt, byte[] verifier, BigInteger N, BigInteger g, BigInteger k) : base(i, salt, verifier, N, g, k) { }

        public override BigInteger CalculateServerEvidence(BigInteger A, BigInteger clientM1, BigInteger K)
        {
            return DoCalculateEvidence(A, clientM1, K);
        }

        public override BigInteger? DoVerifyClientEvidence(BigInteger A, BigInteger clientM1)
        {
            BigInteger N = GetN();
            if ((A % N).IsZero)
                return null;

            BigInteger u = CalculateU(A);
            if ((u % N).IsZero)
                return null;

            BigInteger S = BigInteger.ModPow(A * BigInteger.ModPow(v, u, N), b, N);

            BigInteger ourM = DoCalculateEvidence(A, B, S);

            if (ourM != clientM1)
                return null;

            return S;
        }

        byte[] GetBrokenEvidenceVector(BigInteger bn)
        {
            int bytes = (int)(bn.GetBitLength() + 8) >> 3;
            var byteArray = bn.ToByteArray(true, true);
            return new byte[bytes - byteArray.Length].Combine(byteArray);
        }

        public abstract byte GetVersion();
        public abstract uint GetXIterations();

        public virtual BigInteger CalculateU(BigInteger A) { return default; }

        public virtual BigInteger DoCalculateEvidence(params BigInteger[] bns) { return default; }

        public BigInteger DoCalculateEvidenceHash256(params BigInteger[] bns)
        {
            for (var i = 0; i < bns.Length; ++i)
            {
                var bytes = GetBrokenEvidenceVector(bns[i]);
                if (i == bns.Length - 1)
                {
                    _sha256.TransformFinalBlock(bytes, 0, bytes.Length);
                    break;
                }

                _sha256.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }

            return new BigInteger(_sha256.Hash, true, true);
        }

        public BigInteger DoCalculateEvidenceHash512(params BigInteger[] bns)
        {
            for (var i = 0; i < bns.Length; ++i)
            {
                var bytes = GetBrokenEvidenceVector(bns[i]);
                if (i == bns.Length - 1)
                {
                    _sha512.TransformFinalBlock(bytes, 0, bytes.Length);
                    break;
                }

                _sha512.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }

            return new BigInteger(_sha512.Hash, true, true);
        }
    }

    public class BnetSRP6v1Base : BnetSRP6Base
    {
        /// <summary>
        /// // the modulus, an algorithm parameter; all operations are mod this
        /// </summary>
        public static BigInteger N;

        /// <summary>
        /// // a [g]enerator for the ring of integers mod N, algorithm parameter
        /// </summary>
        public static BigInteger g;

        protected static byte[] dummyBytes = new byte[127];

        static BnetSRP6v1Base()
        {
            g = new BigInteger(2u);
            N = new BigInteger("86A7F6DEEB306CE519770FE37D556F29944132554DED0BD68205E27F3231FEF5A10108238A3150C59CAF7B0B6478691C13A6ACF5E1B5ADAFD4A943D4A21A142B800E8A55F8BFBAC700EB77A7235EE5A609E350EA9FC19F10D921C2FA832E4461B7125D38D254A0BE873DFC27858ACB3F8B9F258461E4373BC3A6C2A9634324AB".ToByteArray(), true, true);
        }

        public BnetSRP6v1Base() : base() { }
        public BnetSRP6v1Base(string username, byte[] salt, byte[] verifier, BigInteger k) : base(new BigInteger(SHA256.HashData(Encoding.UTF8.GetBytes(username)), true), salt, verifier, N, g, k) { }

        public override BigInteger CalculateX(string username, string password, byte[] salt)
        {
            return new BigInteger(SHA256.HashData(salt.Combine(SHA256.HashData(Encoding.UTF8.GetBytes(username + ":" + password)))), true);
        }

        public override BigInteger GetN() { return N; }
        public override BigInteger Getg() { return g; }

        public override byte GetVersion() { return 1; }
        public override uint GetXIterations() { return 1; }
    }

    public class BnetSRP6v2Base : BnetSRP6Base
    {
        /// <summary>
        /// // the modulus, an algorithm parameter; all operations are mod this
        /// </summary>
        protected static BigInteger N;

        /// <summary>
        /// // a [g]enerator for the ring of integers mod N, algorithm parameter
        /// </summary>
        protected static BigInteger g;

        protected static byte[] dummyBytes = new byte[255];

        static BnetSRP6v2Base()
        {
            N = new BigInteger("AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC3192943DB56050A37329CBB4A099ED8193E0757767A13DD52312AB4B03310DCD7F48A9DA04FD50E8083969EDB767B0CF6095179A163AB3661A05FBD5FAAAE82918A9962F0B93B855F97993EC975EEAA80D740ADBF4FF747359D041D5C33EA71D281E446B14773BCA97B43A23FB801676BD207A436C6481F1D2B9078717461A5B9D32E688F87748544523B524B0D57D5EA77A2775D2ECFA032CFBDBF52FB3786160279004E57AE6AF874E7303CE53299CCC041C7BC308D82A5698F3A8D0C38271AE35F8E9DBFBB694B5C803D89F7AE435DE236D525F54759B65E372FCD68EF20FA7111F9E4AFF73".ToByteArray(), true, true);
            g = new BigInteger(2u);
        }

        public BnetSRP6v2Base() : base() { }
        public BnetSRP6v2Base(string username, byte[] salt, byte[] verifier, BigInteger k) : base(new BigInteger(SHA256.HashData(Encoding.UTF8.GetBytes(username)), true), salt, verifier, N, g, k) { }

        public override BigInteger CalculateX(string username, string password, byte[] salt)
        {
            string tmp = username + ":" + password;

            Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(tmp, salt, (int)GetXIterations(), HashAlgorithmName.SHA512);
            byte[] xBytes = rfc.GetBytes(SHA512.HashSizeInBytes);
            BigInteger x = new(xBytes, true, true);
            if ((xBytes[0] & 0x80) != 0)
            {
                byte[] fix = new byte[65];
                fix[64] = 1;
                x -= new BigInteger(fix, true);
            }

            return x % (N - 1);
        }

        public override BigInteger GetN() { return N; }
        public override BigInteger Getg() { return g; }

        public override byte GetVersion() { return 2; }
        public override uint GetXIterations() { return 15000; }
    }

    public class BnetSRP6v1Hash256 : BnetSRP6v1Base
    {
        public BnetSRP6v1Hash256() : base() { }
        public BnetSRP6v1Hash256(string username, byte[] salt, byte[] verifier) : base(username, salt, verifier, new BigInteger(SHA256.HashData(N.ToByteArray(true, true).Combine(dummyBytes.Combine(g.ToByteArray(true, true)))), true, true)) { }

        public override BigInteger CalculateU(BigInteger A)
        {
            return new BigInteger(SHA256.HashData(A.ToByteArray(true, true).Combine(B.ToByteArray(true, true))), true, true);
        }

        public override BigInteger DoCalculateEvidence(params BigInteger[] bns)
        {
            return DoCalculateEvidenceHash256(bns);
        }
    }

    public class BnetSRP6v1Hash512 : BnetSRP6v1Base
    {
        public BnetSRP6v1Hash512() : base() { }
        public BnetSRP6v1Hash512(string username, byte[] salt, byte[] verifier) : base(username, salt, verifier, new BigInteger(SHA512.HashData(N.ToByteArray(true, true).Combine(dummyBytes.Combine(g.ToByteArray(true, true)))), true, true)) { }

        public override BigInteger CalculateU(BigInteger A)
        {
            return new BigInteger(_sha512.ComputeHash(A.ToByteArray(true, true).Combine(B.ToByteArray(true, true))), true, true);
        }

        public override BigInteger DoCalculateEvidence(params BigInteger[] bns)
        {
            return DoCalculateEvidenceHash512(bns);
        }
    }

    public class BnetSRP6v2Hash256 : BnetSRP6v2Base
    {
        public BnetSRP6v2Hash256() : base() { }
        public BnetSRP6v2Hash256(string username, byte[] salt, byte[] verifier) : base(username, salt, verifier, new BigInteger(SHA256.HashData(N.ToByteArray(true, true).Combine(dummyBytes.Combine(g.ToByteArray(true, true)))), true, true)) { }

        public override BigInteger CalculateU(BigInteger A)
        {
            return new BigInteger(SHA256.HashData(A.ToByteArray(true, true).Combine(B.ToByteArray(true, true))), true, true);
        }

        public override BigInteger DoCalculateEvidence(params BigInteger[] bns)
        {
            return DoCalculateEvidenceHash256(bns);
        }
    }

    public class BnetSRP6v2Hash512 : BnetSRP6v2Base
    {
        public BnetSRP6v2Hash512() : base() { }
        public BnetSRP6v2Hash512(string username, byte[] salt, byte[] verifier) : base(username, salt, verifier, new BigInteger(SHA512.HashData(N.ToByteArray(true, true).Combine(g.ToByteArray(true, true))), true, true)) { }

        public override BigInteger CalculateU(BigInteger A)
        {
            return new BigInteger(SHA512.HashData(A.ToByteArray(true, true).Combine(B.ToByteArray(true, true))), true, true);
        }

        public override BigInteger DoCalculateEvidence(params BigInteger[] bns)
        {
            return DoCalculateEvidenceHash512(bns);
        }
    }
}