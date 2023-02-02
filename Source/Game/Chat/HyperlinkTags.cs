// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.IO;
using Game.Entities;

namespace Game.Chat
{
    class HyperlinkDataTokenizer
    {
        public HyperlinkDataTokenizer(string arg, bool allowEmptyTokens = false)
        {
            _arg = new(arg);
            _allowEmptyTokens = allowEmptyTokens;
        }

        public bool TryConsumeTo(out byte val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextByte(":");
            return true;
        }
        public bool TryConsumeTo(out ushort val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }
            val = _arg.NextUInt16(":");
            return true;
        }
        public bool TryConsumeTo(out uint val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextUInt32(":");
            return true;
        }
        public bool TryConsumeTo(out ulong val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextUInt64(":");
            return true;
        }
        public bool TryConsumeTo(out sbyte val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextSByte(":");
            return true;
        }
        public bool TryConsumeTo(out short val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextInt16(":");
            return true;
        }
        public bool TryConsumeTo(out int val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextInt32(":");
            return true;
        }
        public bool TryConsumeTo(out long val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextInt64(":");
            return true;
        }
        public bool TryConsumeTo(out float val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextSingle(":");
            return true;
        }
        public bool TryConsumeTo(out ObjectGuid val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = ObjectGuid.FromString(_arg.NextString(":"));
            return true;
        }
        public bool TryConsumeTo(out string val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextString(":");
            return true;
        }
        public bool TryConsumeTo(out bool val)
        {
            if (IsEmpty())
            {
                val = default;
                return _allowEmptyTokens;
            }

            val = _arg.NextBoolean(":");
            return true;
        }

        public bool IsEmpty() { return _arg.Empty(); }

        StringArguments _arg;
        bool _allowEmptyTokens;
    }
}
