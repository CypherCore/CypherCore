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

using Framework.Constants;
using Framework.GameMath;

namespace Game.DataStorage
{
    public sealed class LFGDungeonsRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string Description;
        public byte MinLevel;
        public ushort MaxLevel;
        public LfgType TypeID;
        public byte Subtype;
        public sbyte Faction;
        public int IconTextureFileID;
        public int RewardsBgTextureFileID;
        public int PopupBgTextureFileID;
        public byte ExpansionLevel;
        public short MapID;
        public Difficulty DifficultyID;
        public float MinGear;
        public byte GroupID;
        public byte OrderIndex;
        public uint RequiredPlayerConditionId;
        public byte TargetLevel;
        public byte TargetLevelMin;
        public ushort TargetLevelMax;
        public ushort RandomID;
        public ushort ScenarioID;
        public ushort FinalEncounterID;
        public byte CountTank;
        public byte CountHealer;
        public byte CountDamage;
        public byte MinCountTank;
        public byte MinCountHealer;
        public byte MinCountDamage;
        public ushort BonusReputationAmount;
        public ushort MentorItemLevel;
        public byte MentorCharLevel;
        public LfgFlags[] Flags = new LfgFlags[2];

        // Helpers
        public uint Entry() { return (uint)(Id + ((int)TypeID << 24)); }
    }

    public sealed class LightRecord
    {
        public uint Id;
        public Vector3 GameCoords;
        public float GameFalloffStart;
        public float GameFalloffEnd;
        public short ContinentID;
        public ushort[] LightParamsID = new ushort[8];
    }

    public sealed class LiquidTypeRecord
    {
        public uint Id;
        public string Name;
        public string[] Texture = new string[6];
        public ushort Flags;
        public byte SoundBank;                                                // used to be "type", maybe needs fixing (works well for now)
        public uint SoundID;
        public uint SpellID;
        public float MaxDarkenDepth;
        public float FogDarkenIntensity;
        public float AmbDarkenIntensity;
        public float DirDarkenIntensity;
        public ushort LightID;
        public float ParticleScale;
        public byte ParticleMovement;
        public byte ParticleTexSlots;
        public byte MaterialID;
        public int MinimapStaticCol;
        public byte[] FrameCountTexture = new byte[6];
        public int[] Color = new int[2];
        public float[] Float = new float[18];
        public uint[] Int = new uint[4];
        public float[] Coefficient = new float[4];
    }

    public sealed class LockRecord
    {
        public uint Id;
        public int[] Index = new int[SharedConst.MaxLockCase];
        public ushort[] Skill = new ushort[SharedConst.MaxLockCase];
        public byte[] LockType = new byte[SharedConst.MaxLockCase];
        public byte[] Action = new byte[SharedConst.MaxLockCase];
    }
}
