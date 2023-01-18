// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Cryptography
{
    public class Sha256
    {
        public Sha256()
        {
            sha = SHA256.Create();
            sha.Initialize();
        }

        public void Process(byte[] data, int length)
        {
            sha.TransformBlock(data, 0, length, data, 0);
        }

        public void Process(uint data)
        {
            var bytes = BitConverter.GetBytes(data);

            sha.TransformBlock(bytes, 0, 4, bytes, 0);
        }

        public void Process(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);

            sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        public void Finish(byte[] data)
        {
            sha.TransformFinalBlock(data, 0, data.Length);

            Digest = sha.Hash;
        }

        public void Finish(byte[] data, int offset, int length)
        {
            sha.TransformFinalBlock(data, offset, length);

            Digest = sha.Hash;
        }

        SHA256 sha;
        public byte[] Digest { get; private set; }
    }

    public class HmacHash : HMACSHA1
    {
        public HmacHash(byte[] key) : base(key)
        {
            Initialize();
        }

        public void Process(byte[] data, int length)
        {
            TransformBlock(data, 0, length, data, 0);
        }

        public void Process(uint data)
        {
            var bytes = BitConverter.GetBytes(data);

            TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        public void Process(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);

            TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        public void Finish(byte[] data, int length)
        {
            TransformFinalBlock(data, 0, length);

            Digest = Hash;
        }

        public void Finish(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);

            TransformFinalBlock(bytes, 0, bytes.Length);

            Digest = Hash;
        }

        public byte[] Digest { get; private set; }
    }

    public class HmacSha256 : HMACSHA256
    {
        public HmacSha256(byte[] key) : base(key)
        {
            Initialize();
        }

        public void Process(byte[] data, int length)
        {
            TransformBlock(data, 0, length, data, 0);
        }

        public void Process(uint data)
        {
            var bytes = BitConverter.GetBytes(data);

            TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        public void Process(string data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);

            TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        public void Finish(byte[] data, int length)
        {
            TransformFinalBlock(data, 0, length);

            Digest = Hash;
        }

        public byte[] Digest { get; private set; }
    }
}
