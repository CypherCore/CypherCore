// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DataStorage
{
    public sealed class EmotesRecord
    {
        public uint Id;
        public long RaceMask;
        public string EmoteSlashCommand;
        public int AnimId;
        public uint EmoteFlags;
        public byte EmoteSpecProc;
        public uint EmoteSpecProcParam;
        public uint EventSoundID;
        public uint SpellVisualKitId;
        public int ClassMask;
    }

    public sealed class EmotesTextRecord
    {
        public uint Id;
        public string Name;
        public ushort EmoteId;
    }

    public sealed class EmotesTextSoundRecord
    {
        public uint Id;
        public byte RaceId;
        public byte ClassId;
        public byte SexId;
        public uint SoundId;
        public uint EmotesTextId;
    }

    public sealed class ExpectedStatRecord
    {
        public uint Id;
        public int ExpansionID;
        public float CreatureHealth;
        public float PlayerHealth;
        public float CreatureAutoAttackDps;
        public float CreatureArmor;
        public float PlayerMana;
        public float PlayerPrimaryStat;
        public float PlayerSecondaryStat;
        public float ArmorConstant;
        public float CreatureSpellDamage;
        public uint Lvl;
    }

    public sealed class ExpectedStatModRecord
    {
        public uint Id;
        public float CreatureHealthMod;
        public float PlayerHealthMod;
        public float CreatureAutoAttackDPSMod;
        public float CreatureArmorMod;
        public float PlayerManaMod;
        public float PlayerPrimaryStatMod;
        public float PlayerSecondaryStatMod;
        public float ArmorConstantMod;
        public float CreatureSpellDamageMod;
    }
}
