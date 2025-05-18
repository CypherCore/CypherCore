// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class LanguageWordsRecord
    {
        public uint Id;
        public string Word;
        public uint LanguageID;
    }

    public sealed class LanguagesRecord
    {
        public LocalizedString Name;
        public uint Id;
        public int Flags;
        public int UiTextureKitID;
        public int UiTextureKitElementCount;
        public int LearningCurveID;
    }

    public sealed class LFGDungeonsRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string Description;
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
        public ushort RandomID;
        public ushort ScenarioID;
        public ushort FinalEncounterID;
        public byte CountTank;
        public byte CountHealer;
        public byte CountDamage;
        public byte MinCountTank;
        public byte MinCountHealer;
        public byte MinCountDamage;
        public byte MaxPremadeCountTank;
        public byte MaxPremadeCountHealer;
        public byte MaxPremadeCountDamage;
        public ushort BonusReputationAmount;
        public ushort MentorItemLevel;
        public byte MentorCharLevel;
        public byte MaxPremadeGroupSize;
        public uint ContentTuningID;
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

    public sealed class LocationRecord
    {
        public uint Id;
        public Vector3 Pos;
        public float[] Rot = new float[3];
    }

    public sealed class LockRecord
    {
        public uint Id;
        public int Flags;
        public int[] Index = new int[SharedConst.MaxLockCase];
        public ushort[] Skill = new ushort[SharedConst.MaxLockCase];
        public byte[] LockType = new byte[SharedConst.MaxLockCase];
        public byte[] Action = new byte[SharedConst.MaxLockCase];
    }
}
