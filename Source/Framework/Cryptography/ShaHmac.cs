/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * Copyright (C) 2012-2014 Arctium Emulation <http://arctium.org>
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
using System.Text;

namespace Framework.Cryptography
{
    public class Sha256
    {
        public Sha256()
        {
            sha = new SHA256Managed();
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
        public HmacHash(byte[] key) : base(key, true)
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
