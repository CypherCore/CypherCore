// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage
{
    public sealed class KeyChainRecord
    {
        public uint Id;
        public byte[] Key = new byte[32];
    }

    public sealed class KeystoneAffixRecord
    {
        public LocalizedString Name;
        public LocalizedString Description;
        public uint Id;
        public int FiledataID;
    }
}