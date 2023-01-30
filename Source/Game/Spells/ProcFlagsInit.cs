// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;

namespace Game.Spells
{
    public class ProcFlagsInit : FlagsArray<int>
    {
        public ProcFlagsInit(ProcFlags procFlags = 0, ProcFlags2 procFlags2 = 0) : base(2)
        {
            _storage[0] = (int)procFlags;
            _storage[1] = (int)procFlags2;
        }

        public ProcFlagsInit(params int[] flags) : base(flags)
        {
        }

        public ProcFlagsInit Or(ProcFlags procFlags)
        {
            _storage[0] |= (int)procFlags;

            return this;
        }

        public ProcFlagsInit Or(ProcFlags2 procFlags2)
        {
            _storage[1] |= (int)procFlags2;

            return this;
        }

        public bool HasFlag(ProcFlags procFlags)
        {
            return (_storage[0] & (int)procFlags) != 0;
        }

        public bool HasFlag(ProcFlags2 procFlags)
        {
            return (_storage[1] & (int)procFlags) != 0;
        }
    }
}