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
using System;

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
        public byte UiLocomotionType;
        public ushort VehicleUIIndicatorID;
        public int MissileTargetingID;
        public ushort[] SeatID = new ushort[8];
        public ushort[] PowerDisplayID = new ushort[3];
    }

    public sealed class VehicleSeatRecord
    {
        public uint Id;
        public Vector3 AttachmentOffset;
        public Vector3 CameraOffset;
        public VehicleSeatFlags Flags;
        public VehicleSeatFlagsB FlagsB;
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

        public bool CanEnterOrExit()
        {
            return (Flags.HasAnyFlag(VehicleSeatFlags.CanEnterOrExit) ||
                //If it has anmation for enter/ride, means it can be entered/exited by logic
                Flags.HasAnyFlag(VehicleSeatFlags.HasLowerAnimForEnter | VehicleSeatFlags.HasLowerAnimForRide));
        }
        public bool CanSwitchFromSeat() { return (Flags & VehicleSeatFlags.CanSwitch) != 0; }
        public bool IsUsableByOverride()
        {
            return Flags.HasAnyFlag(VehicleSeatFlags.Uncontrolled | VehicleSeatFlags.Unk18)
                || FlagsB.HasAnyFlag(VehicleSeatFlagsB.UsableForced | VehicleSeatFlagsB.UsableForced2 |
                    VehicleSeatFlagsB.UsableForced3 | VehicleSeatFlagsB.UsableForced4);
        }
        public bool IsEjectable() { return FlagsB.HasAnyFlag(VehicleSeatFlagsB.Ejectable); }
    }
}
