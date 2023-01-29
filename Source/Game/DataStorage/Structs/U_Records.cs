// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class UiMapRecord
    {
        public int AlternateUiMapGroup;
        public int BkgAtlasID;
        public uint BountyDisplayLocation;
        public int BountySetID;
        public int ContentTuningID;
        public int Flags;
        public sbyte HelpTextPosition;
        public uint Id;
        public LocalizedString Name;
        public int ParentUiMapID;
        public uint System;
        public UiMapType Type;
        public int VisibilityPlayerConditionID;

        public UiMapFlag GetFlags()
        {
            return (UiMapFlag)Flags;
        }
    }

    public sealed class UiMapAssignmentRecord
    {
        public int AreaID;
        public uint Id;
        public int MapID;
        public int OrderIndex;
        public Vector3[] Region = new Vector3[2];
        public int UiMapID;
        public Vector2 UiMax;
        public Vector2 UiMin;
        public int WmoDoodadPlacementID;
        public int WmoGroupID;
    }

    public sealed class UiMapLinkRecord
    {
        public int ChildUiMapID;
        public int Flags;
        public uint Id;
        public int OrderIndex;
        public int OverrideHighlightAtlasID;
        public int OverrideHighlightFileDataID;
        public int ParentUiMapID;
        public int PlayerConditionID;
        public Vector2 UiMax;
        public Vector2 UiMin;
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
        public int AllianceQuestID;
        public string BottomLeftFeatureDesc;
        public string BottomLeftFeatureTitle;
        public int CharLevelConditionID;
        public string Header;
        public int HordeQuestID;
        public uint Id;
        public int PlayerConditionID;
        public int RequiredTimeEventPassed; // serverside TimeEvent table, see ModifierTreeType::HasTimeEventPassed
        public string RightFeatureDesc;
        public string RightFeatureTitle;
        public sbyte ScreenType;
        public int SoundKitID;
        public int TextureKitID;
        public string TopLeftFeatureDesc;
        public string TopLeftFeatureTitle;
    }

    public sealed class UnitConditionRecord
    {
        public byte Flags;
        public uint Id;
        public sbyte[] Op = new sbyte[8];
        public int[] Value = new int[8];
        public byte[] Variable = new byte[8];

        public UnitConditionFlags GetFlags()
        {
            return (UnitConditionFlags)Flags;
        }
    }

    public sealed class UnitPowerBarRecord
    {
        public byte BarType;
        public byte CenterPower;
        public uint[] Color = new uint[6];
        public string Cost;
        public float EndInset;
        public uint[] FileDataID = new uint[6];
        public ushort Flags;
        public uint Id;
        public uint MaxPower;
        public uint MinPower;
        public string Name;
        public string OutOfError;
        public float RegenerationCombat;
        public float RegenerationPeace;
        public float StartInset;
        public uint StartPower;
        public string ToolTip;
    }
}