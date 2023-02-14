// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage
{
    public sealed class NameGenRecord
    {
        public uint Id;
        public string Name;
        public byte RaceID;
        public byte Sex;
    }

    public sealed class NamesProfanityRecord
    {
        public uint Id;
        public string Name;
        public sbyte Language;
    }

    public sealed class NamesReservedRecord
    {
        public uint Id;
        public string Name;
    }

    public sealed class NamesReservedLocaleRecord
    {
        public uint Id;
        public string Name;
        public byte LocaleMask;
    }

    public sealed class NumTalentsAtLevelRecord
    {
        public uint Id;
        public uint NumTalents;
        public uint NumTalentsDeathKnight;
        public uint NumTalentsDemonHunter;
    }
}
