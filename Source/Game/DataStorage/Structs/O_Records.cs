// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
