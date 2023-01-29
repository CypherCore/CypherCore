﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Framework.Cryptography
{
    public class SessionKeyGenerator256
    {
        private byte[] o0 = new byte[32];
        private readonly byte[] o1 = new byte[32];
        private readonly byte[] o2 = new byte[32];

        private readonly SHA256 sh;
        private uint taken;

        public SessionKeyGenerator256(byte[] buff, int size = 0)
        {
            if (size == 0)
                size = buff.Length;

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

        private void FillUp()
        {
            sh.Initialize();
            sh.TransformBlock(o1, 0, 32, o1, 0);
            sh.TransformBlock(o0, 0, 32, o0, 0);
            sh.TransformFinalBlock(o2, 0, 32);
            o0 = sh.Hash;

            taken = 0;
        }
    }

    public class SessionKeyGenerator
    {
        private byte[] o0 = new byte[32];
        private readonly byte[] o1 = new byte[32];
        private readonly byte[] o2 = new byte[32];

        private readonly SHA1 sh;
        private uint taken;

        public SessionKeyGenerator(byte[] buff, int size = 0)
        {
            if (size == 0)
                size = buff.Length;

            int halfSize = size / 2;

            sh = SHA1.Create();
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
                if (taken == 20)
                    FillUp();

                buf[i] = o0[taken];
                taken++;
            }
        }

        private void FillUp()
        {
            sh.Initialize();
            sh.TransformBlock(o1, 0, 20, o1, 0);
            sh.TransformBlock(o0, 0, 20, o0, 0);
            sh.TransformFinalBlock(o2, 0, 20);
            o0 = sh.Hash;

            taken = 0;
        }
    }
}