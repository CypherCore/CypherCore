// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class VehicleRecord
    {
        public uint Id;
        public int Flags;
        public int FlagsB;
        public float TurnSpeed;
        public float PitchSpeed;
        public float PitchMin;
        public float PitchMax;
        public float MouseLookOffsetPitch;
        public float CameraFadeDistScalarMin;
        public float CameraFadeDistScalarMax;
        public float CameraPitchOffset;
        public float FacingLimitRight;
        public float FacingLimitLeft;
        public float CameraYawOffset;
        public ushort VehicleUIIndicatorID;
        public int MissileTargetingID;
        public ushort VehiclePOITypeID;
        public ushort[] SeatID = new ushort[8];
        public ushort[] PowerDisplayID = new ushort[3];

        public bool HasFlag(VehicleFlags vehicleFlags) { return (Flags & (int)vehicleFlags) != 0; }
    }

    public sealed class VehicleSeatRecord
    {
        public uint Id;
        public Vector3 AttachmentOffset;
        public Vector3 CameraOffset;
        public int Flags;
        public int FlagsB;
        public int FlagsC;
        public int AttachmentID;
        public float EnterPreDelay;
        public float EnterSpeed;
        public float EnterGravity;
        public float EnterMinDuration;
        public float EnterMaxDuration;
        public float EnterMinArcHeight;
        public float EnterMaxArcHeight;
        public short EnterAnimStart;
        public short EnterAnimLoop;
        public short RideAnimStart;
        public short RideAnimLoop;
        public short RideUpperAnimStart;
        public short RideUpperAnimLoop;
        public float ExitPreDelay;
        public float ExitSpeed;
        public float ExitGravity;
        public float ExitMinDuration;
        public float ExitMaxDuration;
        public float ExitMinArcHeight;
        public float ExitMaxArcHeight;
        public short ExitAnimStart;
        public short ExitAnimLoop;
        public short ExitAnimEnd;
        public short VehicleEnterAnim;
        public sbyte VehicleEnterAnimBone;
        public short VehicleExitAnim;
        public sbyte VehicleExitAnimBone;
        public short VehicleRideAnimLoop;
        public sbyte VehicleRideAnimLoopBone;
        public sbyte PassengerAttachmentID;
        public float PassengerYaw;
        public float PassengerPitch;
        public float PassengerRoll;
        public float VehicleEnterAnimDelay;
        public float VehicleExitAnimDelay;
        public sbyte VehicleAbilityDisplay;
        public uint EnterUISoundID;
        public uint ExitUISoundID;
        public int UiSkinFileDataID;
        public float CameraEnteringDelay;
        public float CameraEnteringDuration;
        public float CameraExitingDelay;
        public float CameraExitingDuration;
        public float CameraPosChaseRate;
        public float CameraFacingChaseRate;
        public float CameraEnteringZoom;
        public float CameraSeatZoomMin;
        public float CameraSeatZoomMax;
        public short EnterAnimKitID;
        public short RideAnimKitID;
        public short ExitAnimKitID;
        public short VehicleEnterAnimKitID;
        public short VehicleRideAnimKitID;
        public short VehicleExitAnimKitID;
        public short CameraModeID;

        public bool HasFlag(VehicleSeatFlags flag) { return (Flags & (int)flag) != 0; }
        public bool HasFlag(VehicleSeatFlagsB flag) { return (FlagsB & (int)flag) != 0; }

        public bool CanEnterOrExit()
        {
            return (HasFlag(VehicleSeatFlags.CanEnterOrExit) ||
                //If it has anmation for enter/ride, means it can be entered/exited by logic
                HasFlag(VehicleSeatFlags.HasLowerAnimForEnter | VehicleSeatFlags.HasLowerAnimForRide));
        }
        public bool CanSwitchFromSeat() { return Flags.HasAnyFlag((int)VehicleSeatFlags.CanSwitch); }
        public bool IsUsableByOverride()
        {
            return HasFlag(VehicleSeatFlags.Uncontrolled | VehicleSeatFlags.Unk18)
                || HasFlag(VehicleSeatFlagsB.UsableForced | VehicleSeatFlagsB.UsableForced2 |
                    VehicleSeatFlagsB.UsableForced3 | VehicleSeatFlagsB.UsableForced4);
        }
        public bool IsEjectable() { return HasFlag(VehicleSeatFlagsB.Ejectable); }
    }

    public sealed class VignetteRecord
    {
        public uint ID;
        public LocalizedString Name;
        public uint PlayerConditionID;
        public uint VisibleTrackingQuestID;
        public uint QuestFeedbackEffectID;
        public int Flags;
        public float MaxHeight;
        public float MinHeight;
        public sbyte VignetteType;
        public int RewardQuestID;
        public int UiWidgetSetID;
        public int UiMapPinInfoID;
        public sbyte ObjectiveType;

        public bool HasFlag(VignetteFlags vignetteFlags) { return (Flags & (int)vignetteFlags) != 0; }
        public bool IsInfiniteAOI()
        {
            return HasFlag(VignetteFlags.InfiniteAOI | VignetteFlags.ZoneInfiniteAOI);
        }
    }
}
