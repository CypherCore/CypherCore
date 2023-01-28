// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;

namespace Game.DataStorage
{
	public sealed class LanguageWordsRecord
	{
		public uint Id;
		public uint LanguageID;
		public string Word;
	}

	public sealed class LanguagesRecord
	{
		public int Flags;
		public uint Id;
		public LocalizedString Name;
		public int UiTextureKitElementCount;
		public int UiTextureKitID;
	}

	public sealed class LFGDungeonsRecord
	{
		public ushort BonusReputationAmount;
		public uint ContentTuningID;
		public byte CountDamage;
		public byte CountHealer;
		public byte CountTank;
		public string Description;
		public Difficulty DifficultyID;
		public byte ExpansionLevel;
		public sbyte Faction;
		public ushort FinalEncounterID;
		public LfgFlags[] Flags = new LfgFlags[2];
		public byte GroupID;
		public int IconTextureFileID;
		public uint Id;
		public short MapID;
		public byte MentorCharLevel;
		public ushort MentorItemLevel;
		public byte MinCountDamage;
		public byte MinCountHealer;
		public byte MinCountTank;
		public float MinGear;
		public LocalizedString Name;
		public byte OrderIndex;
		public int PopupBgTextureFileID;
		public ushort RandomID;
		public uint RequiredPlayerConditionId;
		public int RewardsBgTextureFileID;
		public ushort ScenarioID;
		public sbyte Subtype;
		public LfgType TypeID;

		// Helpers
		public uint Entry()
		{
			return (uint)(Id + ((int)TypeID << 24));
		}
	}

	public sealed class LightRecord
	{
		public short ContinentID;
		public Vector3 GameCoords;
		public float GameFalloffEnd;
		public float GameFalloffStart;
		public uint Id;
		public ushort[] LightParamsID = new ushort[8];
	}

	public sealed class LiquidTypeRecord
	{
		public float AmbDarkenIntensity;
		public float[] Coefficient = new float[4];
		public int[] Color = new int[2];
		public float DirDarkenIntensity;
		public ushort Flags;
		public float[] Float = new float[18];
		public float FogDarkenIntensity;
		public byte[] FrameCountTexture = new byte[6];
		public uint Id;
		public uint[] Int = new uint[4];
		public ushort LightID;
		public byte MaterialID;
		public float MaxDarkenDepth;
		public int MinimapStaticCol;
		public string Name;
		public byte ParticleMovement;
		public float ParticleScale;
		public byte ParticleTexSlots;
		public byte SoundBank; // used to be "Type", maybe needs fixing (works well for now)
		public uint SoundID;
		public uint SpellID;
		public string[] Texture = new string[6];
	}

	public sealed class LockRecord
	{
		public byte[] Action = new byte[SharedConst.MaxLockCase];
		public int Flags;
		public uint Id;
		public int[] Index = new int[SharedConst.MaxLockCase];
		public byte[] LockType = new byte[SharedConst.MaxLockCase];
		public ushort[] Skill = new ushort[SharedConst.MaxLockCase];
	}
}