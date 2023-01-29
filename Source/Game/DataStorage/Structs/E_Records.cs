// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DataStorage
{
    public sealed class EmotesRecord
    {
        public int AnimId;
        public int ClassMask;
        public uint EmoteFlags;
        public string EmoteSlashCommand;
        public byte EmoteSpecProc;
        public uint EmoteSpecProcParam;
        public uint EventSoundID;
        public uint Id;
        public long RaceMask;
        public uint SpellVisualKitId;
    }

    public sealed class EmotesTextRecord
    {
        public ushort EmoteId;
        public uint Id;
        public string Name;
    }

    public sealed class EmotesTextSoundRecord
    {
        public byte ClassId;
        public uint EmotesTextId;
        public uint Id;
        public byte RaceId;
        public byte SexId;
        public uint SoundId;
    }

    public sealed class ExpectedStatRecord
    {
        public float ArmorConstant;
        public float CreatureArmor;
        public float CreatureAutoAttackDps;
        public float CreatureHealth;
        public float CreatureSpellDamage;
        public int ExpansionID;
        public uint Id;
        public uint Lvl;
        public float PlayerHealth;
        public float PlayerMana;
        public float PlayerPrimaryStat;
        public float PlayerSecondaryStat;
    }

    public sealed class ExpectedStatModRecord
    {
        public float ArmorConstantMod;
        public float CreatureArmorMod;
        public float CreatureAutoAttackDPSMod;
        public float CreatureHealthMod;
        public float CreatureSpellDamageMod;
        public uint Id;
        public float PlayerHealthMod;
        public float PlayerManaMod;
        public float PlayerPrimaryStatMod;
        public float PlayerSecondaryStatMod;
    }
}