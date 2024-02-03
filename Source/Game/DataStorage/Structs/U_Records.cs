﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        public sbyte System;
        public UiMapType Type;
        public int BountySetID;
        public uint BountyDisplayLocation;
        public int VisibilityPlayerConditionID2; // if not met then map is skipped when evaluating UiMapAssignment
        public int VisibilityPlayerConditionID;  // if not met then client checks other maps with the same AlternateUiMapGroup, not re-evaluating UiMapAssignment for them
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
