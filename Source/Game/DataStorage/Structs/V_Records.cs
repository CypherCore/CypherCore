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
using System;

namespace Game.DataStorage
{
    public sealed class VehicleRecord
    {
        public uint Id;
        public VehicleFlags Flags;
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
        public ushort[] SeatID = new ushort[SharedConst.MaxVehicleSeats];
        public ushort VehicleUIIndicatorID;
        public ushort[] PowerDisplayID = new ushort[3];
        public byte FlagsB;
        public byte UiLocomotionType;
        public ushort MissileTargetingID;
    }

    public sealed class VehicleSeatRecord
    {
        public uint Id;
        public VehicleSeatFlags Flags;
        public VehicleSeatFlagsB FlagsB;
        public uint FlagsC;
        public Vector3 AttachmentOffset;
        public float EnterPreDelay;
        public float EnterSpeed;
        public float EnterGravity;
        public float EnterMinDuration;
        public float EnterMaxDuration;
        public float EnterMinArcHeight;
        public float EnterMaxArcHeight;
        public float ExitPreDelay;
        public float ExitSpeed;
        public float ExitGravity;
        public float ExitMinDuration;
        public float ExitMaxDuration;
        public float ExitMinArcHeight;
        public float ExitMaxArcHeight;
        public float PassengerYaw;
        public float PassengerPitch;
        public float PassengerRoll;
        public float VehicleEnterAnimDelay;
        public float VehicleExitAnimDelay;
        public float CameraEnteringDelay;
        public float CameraEnteringDuration;
        public float CameraExitingDelay;
        public float CameraExitingDuration;
        public Vector3 CameraOffset;
        public float CameraPosChaseRate;
        public float CameraFacingChaseRate;
        public float CameraEnteringZoom;
        public float CameraSeatZoomMin;
        public float CameraSeatZoomMax;
        public uint UiSkinFileDataID;
        public short EnterAnimStart;
        public short EnterAnimLoop;
        public short RideAnimStart;
        public short RideAnimLoop;
        public short RideUpperAnimStart;
        public short RideUpperAnimLoop;
        public short ExitAnimStart;
        public short ExitAnimLoop;
        public short ExitAnimEnd;
        public short VehicleEnterAnim;
        public short VehicleExitAnim;
        public short VehicleRideAnimLoop;
        public ushort EnterAnimKitID;
        public ushort RideAnimKitID;
        public ushort ExitAnimKitID;
        public ushort VehicleEnterAnimKitID;
        public ushort VehicleRideAnimKitID;
        public ushort VehicleExitAnimKitID;
        public ushort CameraModeID;
        public sbyte AttachmentID;
        public sbyte PassengerAttachmentID;
        public sbyte VehicleEnterAnimBone;
        public sbyte VehicleExitAnimBone;
        public sbyte VehicleRideAnimLoopBone;
        public byte VehicleAbilityDisplay;
        public uint EnterUISoundID;
        public ushort ExitUISoundID;

        public bool CanEnterOrExit()
        {
            return (Flags.HasAnyFlag(VehicleSeatFlags.CanEnterOrExit) ||
                //If it has anmation for enter/ride, means it can be entered/exited by logic
                Flags.HasAnyFlag(VehicleSeatFlags.HasLowerAnimForEnter | VehicleSeatFlags.HasLowerAnimForRide));
        }
        public bool CanSwitchFromSeat() { return Flags.HasAnyFlag(VehicleSeatFlags.CanSwitch); }
        public bool IsUsableByOverride()
        {
            return Flags.HasAnyFlag(VehicleSeatFlags.Uncontrolled | VehicleSeatFlags.Unk18)
                || FlagsB.HasAnyFlag(VehicleSeatFlagsB.UsableForced | VehicleSeatFlagsB.UsableForced2 |
                    VehicleSeatFlagsB.UsableForced3 | VehicleSeatFlagsB.UsableForced4);
        }
        public bool IsEjectable() { return FlagsB.HasAnyFlag(VehicleSeatFlagsB.Ejectable); }
    }
}
