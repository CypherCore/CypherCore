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

using Framework.IO;
using Game.Entities;

namespace Game.Chat
{
    abstract class HyperLinks<T>
    {
        public abstract string GetTag();
        public abstract bool StoreTo(out T val, string arg);

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    interface IHyperLink<T>
    {
        string GetTag();
        bool StoreTo(out T val, string arg);
    }


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
