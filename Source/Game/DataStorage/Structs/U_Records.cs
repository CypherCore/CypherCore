// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class UiMapRecord
    {
        public LocalizedString Name;
        public uint Id;
        public int ParentUiMapID;
        public int Flags;
        public uint System;
        public UiMapType Type;
        public int BountySetID;
        public uint BountyDisplayLocation;
        public int VisibilityPlayerConditionID;
        public sbyte HelpTextPosition;
        public int BkgAtlasID;
        public int AlternateUiMapGroup;
        public int ContentTuningID;

        public UiMapFlag GetFlags() { return (UiMapFlag)Flags; }
}

    public sealed class UiMapAssignmentRecord
    {
        public Vector2 UiMin;
        public Vector2 UiMax;
        public Vector3[] Region = new Vector3[2];
        public uint Id;
        public int UiMapID;
        public int OrderIndex;
        public int MapID;
        public int AreaID;
        public int WmoDoodadPlacementID;
        public int WmoGroupID;
    }

    public sealed class UiMapLinkRecord
    {
        public Vector2 UiMin;
        public Vector2 UiMax;
        public uint Id;
        public int ParentUiMapID;
        public int OrderIndex;
        public int ChildUiMapID;
        public int PlayerConditionID;
        public int OverrideHighlightFileDataID;
        public int OverrideHighlightAtlasID;
        public int Flags;
    }

    public sealed class UiMapXMapArtRecord
    {
        public uint Id;
        public int PhaseID;
        public int UiMapArtID;
        public uint UiMapID;
    }

    public sealed class UISplashScreenRecord
    {
        public uint Id;
        public string Header;
        public string TopLeftFeatureTitle;
        public string TopLeftFeatureDesc;
        public string BottomLeftFeatureTitle;
        public string BottomLeftFeatureDesc;
        public string RightFeatureTitle;
        public string RightFeatureDesc;
        public int AllianceQuestID;
        public int HordeQuestID;
        public sbyte ScreenType;
        public int TextureKitID;
        public int SoundKitID;
        public int PlayerConditionID;
        public int CharLevelConditionID;
        public int RequiredTimeEventPassed; // serverside TimeEvent table, see ModifierTreeType::HasTimeEventPassed
    }

    public sealed class UnitConditionRecord
    {
        public uint Id;
        public byte Flags;
        public byte[] Variable = new byte[8];
        public sbyte[] Op = new sbyte[8];
        public int[] Value = new int[8];

        public UnitConditionFlags GetFlags() { return (UnitConditionFlags)Flags; }
    }

    public sealed class UnitPowerBarRecord
    {
        public uint Id;
        public string Name;
        public string Cost;
        public string OutOfError;
        public string ToolTip;
        public uint MinPower;
        public uint MaxPower;
        public uint StartPower;
        public byte CenterPower;
        public float RegenerationPeace;
        public float RegenerationCombat;
        public byte BarType;
        public ushort Flags;
        public float StartInset;
        public float EndInset;
        public uint[] FileDataID = new uint[6];
        public uint[] Color = new uint[6];
    }
}
