/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Framework.GameMath;

namespace Game.DataStorage
{
    public sealed class LFGDungeonsRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string Description;
        public LfgFlags Flags;
        public float MinItemLevel;
        public ushort MaxLevel;
        public ushort TargetLevelMax;
        public short MapID;
        public ushort RandomID;
        public ushort ScenarioID;
        public ushort LastBossJournalEncounterID;
        public ushort BonusReputationAmount;
        public ushort MentorItemLevel;
        public ushort PlayerConditionID;
        public byte MinLevel;
        public byte TargetLevel;
        public byte TargetLevelMin;
        public Difficulty DifficultyID;
        public LfgType Type;
        public byte Faction;
        public byte Expansion;
        public byte OrderIndex;
        public byte GroupID;
        public byte CountTank;
        public byte CountHealer;
        public byte CountDamage;
        public byte MinCountTank;
        public byte MinCountHealer;
        public byte MinCountDamage;
        public byte SubType;
        public byte MentorCharLevel;
        public int TextureFileDataID;
        public int RewardIconFileDataID;
        public int ProposalTextureFileDataID;

        // Helpers
        public uint Entry() { return (uint)(Id + ((int)Type << 24)); }
    }

    public sealed class LightRecord
    {
        public uint Id;
        public Vector3 Pos;
        public float FalloffStart;
        public float FalloffEnd;
        public ushort MapID;
        public ushort[] LightParamsID = new ushort[8];
    }

    public sealed class LiquidTypeRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string[] Texture = new string[6];
        public uint SpellID;
        public float MaxDarkenDepth;
        public float FogDarkenIntensity;
        public float AmbDarkenIntensity;
        public float DirDarkenIntensity;
        public float ParticleScale;
        public uint[] Color = new uint[2];
        public float[] Float = new float[18];
        public uint[] Int = new uint[4];
        public ushort Flags;
        public ushort LightID;
        public byte LiquidType;
        public byte ParticleMovement;
        public byte ParticleTexSlots;
        public byte MaterialID;
        public byte[] DepthTexCount = new byte[6];
        public ushort SoundID;
    }

    public sealed class LockRecord
    {
        public uint Id;
        public uint[] Index = new uint[SharedConst.MaxLockCase];
        public ushort[] Skill = new ushort[SharedConst.MaxLockCase];
        public byte[] LockType = new byte[SharedConst.MaxLockCase];
        public byte[] Action = new byte[SharedConst.MaxLockCase];
    }
}
