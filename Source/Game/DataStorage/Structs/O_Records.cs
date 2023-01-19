// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class OverrideSpellDataRecord
    {
        public uint Id;
        public uint[] Spells = new uint[SharedConst.MaxOverrideSpell];
        public uint PlayerActionBarFileDataID;
        public byte Flags;
    }
}
