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
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Maps;
using Game.Movement;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.MoveChangeTransport, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveDoubleJump, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveFallLand, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveFallReset, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveHeartbeat, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveJump, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetFacing, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetFly, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetPitch, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetRunMode, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetWalkMode, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartAscend, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartBackward, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartDescend, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartForward, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartPitchDown, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartPitchUp, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartStrafeLeft, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartStrafeRight, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartSwim, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartTurnLeft, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStartTurnRight, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStop, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopAscend, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopPitch, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopStrafe, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopSwim, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveStopTurn, Processing = PacketProcessing.ThreadSafe)]
        void HandleMovement(ClientPlayerMovement packet)
        {
            HandleMovementOpcode(packet.GetOpcode(), packet.Status);
        }

        void HandleMovementOpcode(ClientOpcodes opcode, MovementInfo movementInfo)
        {
            Unit mover = GetPlayer().m_unitMovedByMe;
            Player plrMover = mover.ToPlayer();

            if (plrMover && plrMover.IsBeingTeleported())
                return;

            GetPlayer().ValidateMovementInfo(movementInfo);

            if (movementInfo.Guid != mover.GetGUID())
            {
                Log.outError(LogFilter.Network, "HandleMovementOpcodes: guid error");
                return;
            }
            if (!movementInfo.Pos.IsPositionValid())
            {
                Log.outError(LogFilter.Network, "HandleMovementOpcodes: Invalid Position");
                return;
            }

            // stop some emotes at player move
            if (plrMover && (plrMover.GetUInt32Value(UnitFields.NpcEmotestate) != 0))
                plrMover.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotNone);

            //handle special cases
            if (!movementInfo.transport.guid.IsEmpty())
            {
                if (movementInfo.transport.pos.GetPositionX() > 50 || movementInfo.transport.pos.GetPositionY() > 50 || movementInfo.transport.pos.GetPositionZ() > 50)
                    return;

                if (!GridDefines.IsValidMapCoord(movementInfo.Pos.posX + movementInfo.transport.pos.posX, movementInfo.Pos.posY + movementInfo.transport.pos.posY,
                    movementInfo.Pos.posZ + movementInfo.transport.pos.posZ, movementInfo.Pos.Orientation + movementInfo.transport.pos.Orientation))
                    return;

                if (plrMover)
                {
                    if (!plrMover.GetTransport())
                    {
                        Transport transport = plrMover.GetMap().GetTransport(movementInfo.transport.guid);
                        if (transport)
                            transport.AddPassenger(plrMover);
                    }
                    else if (plrMover.GetTransport().GetGUID() != movementInfo.transport.guid)
                    {
                        plrMover.GetTransport().RemovePassenger(plrMover);
                        Transport transport = plrMover.GetMap().GetTransport(movementInfo.transport.guid);
                        if (transport)
                            transport.AddPassenger(plrMover);
                        else
                            movementInfo.ResetTransport();
                    }
                }

                if (!mover.GetTransport() && !mover.GetVehicle())
                {
                    GameObject go = mover.GetMap().GetGameObject(movementInfo.transport.guid);
                    if (!go || go.GetGoType() != GameObjectTypes.Transport)
                        movementInfo.transport.Reset();
                }
            }
            else if (plrMover && plrMover.GetTransport())                // if we were on a transport, leave
                plrMover.GetTransport().RemovePassenger(plrMover);

            // fall damage generation (ignore in flight case that can be triggered also at lags in moment teleportation to another map).
            if (opcode == ClientOpcodes.MoveFallLand && plrMover && !plrMover.IsInFlight())
                plrMover.HandleFall(movementInfo);

            if (plrMover && movementInfo.HasMovementFlag(MovementFlag.Swimming) != plrMover.IsInWater())
            {
                // now client not include swimming flag in case jumping under water
                plrMover.SetInWater(!plrMover.IsInWater() || plrMover.GetMap().IsUnderWater(plrMover.GetPhaseShift(), movementInfo.Pos.posX, movementInfo.Pos.posY, movementInfo.Pos.posZ));
            }

            uint mstime = Time.GetMSTime();

            if (m_clientTimeDelay == 0)
                m_clientTimeDelay = mstime - movementInfo.Time;

            movementInfo.Time = movementInfo.Time + m_clientTimeDelay;

            movementInfo.Guid = mover.GetGUID();
            mover.m_movementInfo = movementInfo;

            // Some vehicles allow the passenger to turn by himself
            Vehicle vehicle = mover.GetVehicle();
            if (vehicle)
            {
                VehicleSeatRecord seat = vehicle.GetSeatForPassenger(mover);
                if (seat != null)
                {
                    if (seat.Flags.HasAnyFlag(VehicleSeatFlags.AllowTurning))
                    {
                        if (movementInfo.Pos.GetOrientation() != mover.GetOrientation())
                        {
                            mover.SetOrientation(movementInfo.Pos.GetOrientation());
                            mover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Turning);
                        }
                    }
                }
                return;
            }

            mover.UpdatePosition(movementInfo.Pos);

            MoveUpdate moveUpdate = new MoveUpdate();
            moveUpdate.Status = mover.m_movementInfo;
            mover.SendMessageToSet(moveUpdate, GetPlayer());

            if (plrMover)                                            // nothing is charmed, or player charmed
            {
                if (plrMover.IsSitState() && movementInfo.HasMovementFlag(MovementFlag.MaskMoving | MovementFlag.MaskTurning))
                    plrMover.SetStandState(UnitStandStateType.Stand);

                plrMover.UpdateFallInformationIfNeed(movementInfo, opcode);

                if (movementInfo.Pos.posZ < plrMover.GetMap().GetMinHeight(movementInfo.Pos.GetPositionX(), movementInfo.Pos.GetPositionY()))
                {
                    if (!(plrMover.GetBattleground() && plrMover.GetBattleground().HandlePlayerUnderMap(GetPlayer())))
                    {
                        // NOTE: this is actually called many times while falling
                        // even after the player has been teleported away
                        // @todo discard movement packets after the player is rooted
                        if (plrMover.IsAlive())
                        {
                            plrMover.SetFlag(PlayerFields.Flags, PlayerFlags.IsOutOfBounds);
                            plrMover.EnvironmentalDamage(EnviromentalDamage.FallToVoid, (uint)GetPlayer().GetMaxHealth());
                            // player can be alive if GM/etc
                            // change the death state to CORPSE to prevent the death timer from
                            // starting in the next player update
                            if (plrMover.IsAlive())
                                plrMover.KillPlayer();
                        }
                    }
                }
                else
                    plrMover.RemoveFlag(PlayerFields.Flags, PlayerFlags.IsOutOfBounds);

                if (opcode == ClientOpcodes.MoveJump)
                { 
                    plrMover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Jump, 605); // Mind Control
                    plrMover.ProcSkillsAndAuras(null, ProcFlags.Jump, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.WorldPortResponse, Status = SessionStatus.Transfer)]
        void HandleMoveWorldportAck(WorldPortResponse packet)
        {
            HandleMoveWorldportAck();
        }

        void HandleMoveWorldportAck()
        {
            // ignore unexpected far teleports
            if (!GetPlayer().IsBeingTeleportedFar())
                return;

            bool seamlessTeleport = GetPlayer().IsBeingTeleportedSeamlessly();
            GetPlayer().SetSemaphoreTeleportFar(false);

            // get the teleport destination
            WorldLocation loc = GetPlayer().GetTeleportDest();

            // possible errors in the coordinate validity check
            if (!GridDefines.IsValidMapCoord(loc))
            {
                LogoutPlayer(false);
                return;
            }

            // get the destination map entry, not the current one, this will fix homebind and reset greeting
            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(loc.GetMapId());
            InstanceTemplate mInstance = Global.ObjectMgr.GetInstanceTemplate(loc.GetMapId());

            // reset instance validity, except if going to an instance inside an instance
            if (!GetPlayer().m_InstanceValid && mInstance == null)
                GetPlayer().m_InstanceValid = true;

            Map oldMap = GetPlayer().GetMap();
            Map newMap = Global.MapMgr.CreateMap(loc.GetMapId(), GetPlayer());

            if (GetPlayer().IsInWorld)
            {
                Log.outError(LogFilter.Network, "Player (Name {0}) is still in world when teleported from map {1} to new map {2}", GetPlayer().GetName(), oldMap.GetId(), loc.GetMapId());
                oldMap.RemovePlayerFromMap(GetPlayer(), false);
            }

            // relocate the player to the teleport destination
            // the CannotEnter checks are done in TeleporTo but conditions may change
            // while the player is in transit, for example the map may get full
            if (newMap == null || newMap.CannotEnter(GetPlayer()) != 0)
            {
                Log.outError(LogFilter.Network, "Map {0} could not be created for {1} ({2}), porting player to homebind", loc.GetMapId(), newMap ? newMap.GetMapName() : "Unknown", GetPlayer().GetGUID().ToString());
                GetPlayer().TeleportTo(GetPlayer().GetHomebind());
                return;
            }

            float z = loc.GetPositionZ();
            if (GetPlayer().HasUnitMovementFlag(MovementFlag.Hover))
                z += GetPlayer().GetFloatValue(UnitFields.HoverHeight);

            GetPlayer().Relocate(loc.GetPositionX(), loc.GetPositionY(), z, loc.GetOrientation());

            GetPlayer().ResetMap();
            GetPlayer().SetMap(newMap);

            ResumeToken resumeToken = new ResumeToken();
            resumeToken.SequenceIndex = _player.m_movementCounter;
            resumeToken.Reason = seamlessTeleport ? 2 : 1u;
            SendPacket(resumeToken);

            if (!seamlessTeleport)
                GetPlayer().SendInitialPacketsBeforeAddToMap();

            if (!GetPlayer().GetMap().AddPlayerToMap(GetPlayer(), !seamlessTeleport))
            {
                Log.outError(LogFilter.Network, "WORLD: failed to teleport player {0} ({1}) to map {2} ({3}) because of unknown reason!",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString(), loc.GetMapId(), newMap ? newMap.GetMapName() : "Unknown");
                GetPlayer().ResetMap();
                GetPlayer().SetMap(oldMap);
                GetPlayer().TeleportTo(GetPlayer().GetHomebind());
                return;
            }

            // Battleground state prepare (in case join to BG), at relogin/tele player not invited
            // only add to bg group and object, if the player was invited (else he entered through command)
            if (GetPlayer().InBattleground())
            {
                // cleanup setting if outdated
                if (!mapEntry.IsBattlegroundOrArena())
                {
                    // We're not in BG
                    GetPlayer().SetBattlegroundId(0, BattlegroundTypeId.None);
                    // reset destination bg team
                    GetPlayer().SetBGTeam(0);
                }
                // join to bg case
                else
                {
                    Battleground bg = GetPlayer().GetBattleground();
                    if (bg)
                    {
                        if (GetPlayer().IsInvitedForBattlegroundInstance(GetPlayer().GetBattlegroundId()))
                            bg.AddPlayer(GetPlayer());
                    }
                }
            }

            if (!seamlessTeleport)
                GetPlayer().SendInitialPacketsAfterAddToMap();
            else
            {
                GetPlayer().UpdateVisibilityForPlayer();
                Garrison garrison = GetPlayer().GetGarrison();
                if (garrison != null)
                    garrison.SendRemoteInfo();
            }

            // flight fast teleport case
            if (GetPlayer().GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Flight)
            {
                if (!GetPlayer().InBattleground())
                {
                    if (!seamlessTeleport)
                    {
                        // short preparations to continue flight
                        FlightPathMovementGenerator flight = (FlightPathMovementGenerator)GetPlayer().GetMotionMaster().top();
                        flight.Initialize(GetPlayer());
                    }
                    return;
                }

                // Battlegroundstate prepare, stop flight
                GetPlayer().GetMotionMaster().MovementExpired();
                GetPlayer().CleanupAfterTaxiFlight();
            }

            // resurrect character at enter into instance where his corpse exist after add to map
            if (mapEntry.IsDungeon() && !GetPlayer().IsAlive())
            { 
                if (GetPlayer().GetCorpseLocation().GetMapId() == mapEntry.Id)
                {
                    GetPlayer().ResurrectPlayer(0.5f, false);
                    GetPlayer().SpawnCorpseBones();
                }
            }

            bool allowMount = !mapEntry.IsDungeon() || mapEntry.IsBattlegroundOrArena();
            if (mInstance != null)
            {
                // check if this instance has a reset time and send it to player if so
                Difficulty diff = newMap.GetDifficultyID();
                MapDifficultyRecord mapDiff = Global.DB2Mgr.GetMapDifficultyData(mapEntry.Id, diff);
                if (mapDiff != null)
                {
                    if (mapDiff.GetRaidDuration() != 0)
                    {
                        long timeReset = Global.InstanceSaveMgr.GetResetTimeFor(mapEntry.Id, diff);
                        if (timeReset != 0)
                        {
                            uint timeleft = (uint)(timeReset - Time.UnixTime);
                            GetPlayer().SendInstanceResetWarning(mapEntry.Id, diff, timeleft, true);
                        }
                    }
                }

                // check if instance is valid
                if (!GetPlayer().CheckInstanceValidity(false))
                    GetPlayer().m_InstanceValid = false;

                // instance mounting is handled in InstanceTemplate
                allowMount = mInstance.AllowMount;
            }

            // mount allow check
            if (!allowMount)
                GetPlayer().RemoveAurasByType(AuraType.Mounted);

            // update zone immediately, otherwise leave channel will cause crash in mtmap
            uint newzone, newarea;
            GetPlayer().GetZoneAndAreaId(out newzone, out newarea);
            GetPlayer().UpdateZone(newzone, newarea);

            // honorless target
            if (GetPlayer().pvpInfo.IsHostile)
                GetPlayer().CastSpell(GetPlayer(), 2479, true);

            // in friendly area
            else if (GetPlayer().IsPvP() && !GetPlayer().HasFlag(PlayerFields.Flags, PlayerFlags.InPVP))
                GetPlayer().UpdatePvP(false, false);

            // resummon pet
            GetPlayer().ResummonPetTemporaryUnSummonedIfAny();

            //lets process all delayed operations on successful teleport
            GetPlayer().ProcessDelayedOperations();
        }

        [WorldPacketHandler(ClientOpcodes.SuspendTokenResponse, Status = SessionStatus.Transfer)]
        void HandleSuspendTokenResponse(SuspendTokenResponse suspendTokenResponse)
        {
            if (!_player.IsBeingTeleportedFar())
                return;

            WorldLocation loc = GetPlayer().GetTeleportDest();

            if (CliDB.MapStorage.LookupByKey(loc.GetMapId()).IsDungeon())
            {
                UpdateLastInstance updateLastInstance = new UpdateLastInstance();
                updateLastInstance.MapID = loc.GetMapId();
                SendPacket(updateLastInstance);
            }

            NewWorld packet = new NewWorld();
            packet.MapID = loc.GetMapId();
            packet.Pos = loc;
            packet.Reason = (uint)(!_player.IsBeingTeleportedSeamlessly() ? NewWorldReason.Normal : NewWorldReason.Seamless);
            SendPacket(packet);

            if (_player.IsBeingTeleportedSeamlessly())
                HandleMoveWorldportAck();
        }

        [WorldPacketHandler(ClientOpcodes.MoveTeleportAck, Processing = PacketProcessing.ThreadSafe)]
        void HandleMoveTeleportAck(MoveTeleportAck packet)
        {
            Player plMover = GetPlayer().m_unitMovedByMe.ToPlayer();

            if (!plMover || !plMover.IsBeingTeleportedNear())
                return;

            if (packet.MoverGUID != plMover.GetGUID())
                return;

            plMover.SetSemaphoreTeleportNear(false);

            uint old_zone = plMover.GetZoneId();

            WorldLocation dest = plMover.GetTeleportDest();

            plMover.UpdatePosition(dest, true);

            uint newzone, newarea;
            plMover.GetZoneAndAreaId(out newzone, out newarea);
            plMover.UpdateZone(newzone, newarea);

            // new zone
            if (old_zone != newzone)
            {
                // honorless target
                if (plMover.pvpInfo.IsHostile)
                    plMover.CastSpell(plMover, 2479, true);

                // in friendly area
                else if (plMover.IsPvP() && !plMover.HasFlag(PlayerFields.Flags, PlayerFlags.InPVP))
                    plMover.UpdatePvP(false, false);
            }

            // resummon pet
            GetPlayer().ResummonPetTemporaryUnSummonedIfAny();

            //lets process all delayed operations on successful teleport
            GetPlayer().ProcessDelayedOperations();
        }

        [WorldPacketHandler(ClientOpcodes.MoveForceFlightBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceFlightSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForcePitchRateChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceRunBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceRunSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceSwimBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceSwimSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceTurnRateChangeAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceWalkSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
        void HandleForceSpeedChangeAck(MovementSpeedAck packet)
        {
            GetPlayer().ValidateMovementInfo(packet.Ack.Status);

            // now can skip not our packet
            if (GetPlayer().GetGUID() != packet.Ack.Status.Guid)
                return;

            /*----------------*/
            // client ACK send one packet for mounted/run case and need skip all except last from its
            // in other cases anti-cheat check can be fail in false case
            UnitMoveType move_type;

            ClientOpcodes opcode = packet.GetOpcode();
            switch (opcode)
            {
                case ClientOpcodes.MoveForceWalkSpeedChangeAck:
                    move_type = UnitMoveType.Walk;
                    break;
                case ClientOpcodes.MoveForceRunSpeedChangeAck:
                    move_type = UnitMoveType.Run;
                    break;
                case ClientOpcodes.MoveForceRunBackSpeedChangeAck:
                    move_type = UnitMoveType.RunBack;
                    break;
                case ClientOpcodes.MoveForceSwimSpeedChangeAck:
                    move_type = UnitMoveType.Swim;
                    break;
                case ClientOpcodes.MoveForceSwimBackSpeedChangeAck:
                    move_type = UnitMoveType.SwimBack;
                    break;
                case ClientOpcodes.MoveForceTurnRateChangeAck:
                    move_type = UnitMoveType.TurnRate;
                    break;
                case ClientOpcodes.MoveForceFlightSpeedChangeAck:
                    move_type = UnitMoveType.Flight;
                    break;
                case ClientOpcodes.MoveForceFlightBackSpeedChangeAck:
                    move_type = UnitMoveType.FlightBack;
                    break;
                case ClientOpcodes.MoveForcePitchRateChangeAck:
                    move_type = UnitMoveType.PitchRate;
                    break;
                default:
                    Log.outError(LogFilter.Network, "WorldSession.HandleForceSpeedChangeAck: Unknown move type opcode: {0}", opcode);
                    return;
            }

            // skip all forced speed changes except last and unexpected
            // in run/mounted case used one ACK and it must be skipped. m_forced_speed_changes[MOVE_RUN] store both.
            if (GetPlayer().m_forced_speed_changes[(int)move_type] > 0)
            {
                --GetPlayer().m_forced_speed_changes[(int)move_type];
                if (GetPlayer().m_forced_speed_changes[(int)move_type] > 0)
                    return;
            }

            if (!GetPlayer().GetTransport() && Math.Abs(GetPlayer().GetSpeed(move_type) - packet.Speed) > 0.01f)
            {
                if (GetPlayer().GetSpeed(move_type) > packet.Speed)         // must be greater - just correct
                {
                    Log.outError(LogFilter.Network, "{0}SpeedChange player {1} is NOT correct (must be {2} instead {3}), force set to correct value",
                        move_type, GetPlayer().GetName(), GetPlayer().GetSpeed(move_type), packet.Speed);
                    GetPlayer().SetSpeedRate(move_type, GetPlayer().GetSpeedRate(move_type));
                }
                else                                                // must be lesser - cheating
                {
                    Log.outDebug(LogFilter.Server, "Player {0} from account id {1} kicked for incorrect speed (must be {2} instead {3})",
                        GetPlayer().GetName(), GetPlayer().GetSession().GetAccountId(), GetPlayer().GetSpeed(move_type), packet.Speed);
                    GetPlayer().GetSession().KickPlayer();
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetActiveMover)]
        void HandleSetActiveMover(SetActiveMover packet)
        {
            if (GetPlayer().IsInWorld)
            {
                if (_player.m_unitMovedByMe.GetGUID() != packet.ActiveMover)
                    Log.outError(LogFilter.Network, "HandleSetActiveMover: incorrect mover guid: mover is {0} and should be {1},", packet.ActiveMover.ToString(), _player.m_unitMovedByMe.GetGUID().ToString());
            }
        }

        [WorldPacketHandler(ClientOpcodes.MoveKnockBackAck, Processing = PacketProcessing.ThreadSafe)]
        void HandleMoveKnockBackAck(MoveKnockBackAck movementAck)
        {
            GetPlayer().ValidateMovementInfo(movementAck.Ack.Status);

            if (GetPlayer().m_unitMovedByMe.GetGUID() != movementAck.Ack.Status.Guid)
                return;

            GetPlayer().m_movementInfo = movementAck.Ack.Status;

            MoveUpdateKnockBack updateKnockBack = new MoveUpdateKnockBack();
            updateKnockBack.Status = GetPlayer().m_movementInfo;
            GetPlayer().SendMessageToSet(updateKnockBack, false);
        }

        [WorldPacketHandler(ClientOpcodes.MoveEnableDoubleJumpAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveEnableSwimToFlyTransAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveFeatherFallAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceRootAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveForceUnrootAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveGravityDisableAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveGravityEnableAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveHoverAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetCanFlyAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetCanTurnWhileFallingAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveSetIgnoreMovementForcesAck, Processing = PacketProcessing.ThreadSafe)]
        [WorldPacketHandler(ClientOpcodes.MoveWaterWalkAck, Processing = PacketProcessing.ThreadSafe)]
        void HandleMovementAckMessage(MovementAckMessage movementAck)
        {
            GetPlayer().ValidateMovementInfo(movementAck.Ack.Status);
        }

        [WorldPacketHandler(ClientOpcodes.SummonResponse)]
        void HandleSummonResponseOpcode(SummonResponse packet)
        {
            if (!GetPlayer().IsAlive() || GetPlayer().IsInCombat())
                return;

            GetPlayer().SummonIfPossible(packet.Accept);
        }

        [WorldPacketHandler(ClientOpcodes.MoveSetCollisionHeightAck, Processing = PacketProcessing.ThreadSafe)]
        void HandleSetCollisionHeightAck(MoveSetCollisionHeightAck packet)
        {
            GetPlayer().ValidateMovementInfo(packet.Data.Status);
        }

        [WorldPacketHandler(ClientOpcodes.MoveTimeSkipped, Processing = PacketProcessing.Inplace)]
        void HandleMoveTimeSkipped(MoveTimeSkipped moveTimeSkipped)
        {
        }

        [WorldPacketHandler(ClientOpcodes.MoveSplineDone, Processing = PacketProcessing.ThreadSafe)]
        void HandleMoveSplineDoneOpcode(MoveSplineDone moveSplineDone)
        {
            MovementInfo movementInfo = moveSplineDone.Status;
            _player.ValidateMovementInfo(movementInfo);

            // in taxi flight packet received in 2 case:
            // 1) end taxi path in far (multi-node) flight
            // 2) switch from one map to other in case multim-map taxi path
            // we need process only (1)

            uint curDest = GetPlayer().m_taxi.GetTaxiDestination();
            if (curDest != 0)
            {
                TaxiNodesRecord curDestNode = CliDB.TaxiNodesStorage.LookupByKey(curDest);

                // far teleport case
                if (curDestNode != null && curDestNode.ContinentID != GetPlayer().GetMapId())
                {
                    if (GetPlayer().GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Flight)
                    {
                        // short preparations to continue flight
                        FlightPathMovementGenerator flight = (FlightPathMovementGenerator)GetPlayer().GetMotionMaster().top();

                        flight.SetCurrentNodeAfterTeleport();
                        TaxiPathNodeRecord node = flight.GetPath()[(int)flight.GetCurrentNode()];
                        flight.SkipCurrentNode();

                        GetPlayer().TeleportTo(curDestNode.ContinentID, node.Loc.X, node.Loc.Y, node.Loc.Z, GetPlayer().GetOrientation());
                    }
                }

                return;
            }

            // at this point only 1 node is expected (final destination)
            if (GetPlayer().m_taxi.GetPath().Count != 1)
                return;

            GetPlayer().CleanupAfterTaxiFlight();
            GetPlayer().SetFallInformation(0, GetPlayer().GetPositionZ());
            if (GetPlayer().pvpInfo.IsHostile)
                GetPlayer().CastSpell(GetPlayer(), 2479, true);
        }
    }
}
