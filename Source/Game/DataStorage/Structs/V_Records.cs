// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using System;
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class VehicleRecord
    {
        public uint Id;
        public VehicleFlags Flags;
        public byte FlagsB;
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
    }

    public sealed class VehicleSeatRecord
    {
        public uint Id;
        public Vector3 AttachmentOffset;
        public Vector3 CameraOffset;
        public int Flags;
        public int FlagsB;
        public int FlagsC;
        public sbyte AttachmentID;
        public float EnterPreDelay;
        public float EnterSpeed;
        public float EnterGravity;
        public float EnterMinDuration;
        public float EnterMaxDuration;
        public float EnterMinArcHeight;
        public float EnterMaxArcHeight;
        public int EnterAnimStart;
        public int EnterAnimLoop;
        public int RideAnimStart;
        public int RideAnimLoop;
        public int RideUpperAnimStart;
        public int RideUpperAnimLoop;
        public float ExitPreDelay;
        public float ExitSpeed;
        public float ExitGravity;
        public float ExitMinDuration;
        public float ExitMaxDuration;
        public float ExitMinArcHeight;
        public float ExitMaxArcHeight;
        public int ExitAnimStart;
        public int ExitAnimLoop;
        public int ExitAnimEnd;
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

        public bool HasFlag(VehicleSeatFlags flag)
        {
            return Flags.HasAnyFlag((int)flag);
        }

        public bool HasFlag(VehicleSeatFlagsB flag)
        {
            return FlagsB.HasAnyFlag((int)flag);
        }

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
}
