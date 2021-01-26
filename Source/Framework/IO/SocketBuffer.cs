using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.IO
{
    public class SocketBuffer
    {
        byte[] _storage;
        int _wpos;

        public SocketBuffer(int initialSize = 0)
        {
            _storage = new byte[initialSize];
        }

        public void Resize(int bytes)
        {
            _storage = new byte[bytes];
        }

        public byte[] GetData()
        {
            return _storage;
        }

        public void Write(byte[] data, int index, int size)
        {
            Buffer.BlockCopy(data, index, _storage, _wpos, size);
            _wpos += size;
        }

        public int GetRemainingSpace() { return _storage.Length - _wpos; }

        public void Reset()
        {
            _wpos = 0;
        }
    }
}
