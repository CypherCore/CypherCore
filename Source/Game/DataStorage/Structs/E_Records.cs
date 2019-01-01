/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
