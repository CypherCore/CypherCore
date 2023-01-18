// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
