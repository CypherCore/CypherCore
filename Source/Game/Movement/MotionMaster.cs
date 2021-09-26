/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Movement
{
    public class MotionMaster
    {
        public const double gravity = 19.29110527038574;
        public const float SPEED_CHARGE = 42.0f;
        IdleMovementGenerator staticIdleMovement = new();

        public MotionMaster(Unit me)
        {
            _owner = me;
            _top = -1;
            _cleanFlag = MotionMasterCleanFlag.None;

            for (byte i = 0; i < (int)MovementSlot.Max; ++i)
            {
                _slot[i] = null;
                _initialize[i] = true;
            }
        }

        public IMovementGenerator Top()
        {
            Cypher.Assert(!Empty());
            return _slot[_top];
        }

        public void Initialize()
        {
            while (!Empty())
            {
                IMovementGenerator curr = Top();
                Pop();
                if (curr != null)
                    DirectDelete(curr);
            }

            InitDefault();
        }

        public void InitDefault()
        {
            if (_owner.IsTypeId(TypeId.Unit))
            {
                IMovementGenerator movement = AISelector.SelectMovementAI(_owner);
                StartMovement(movement ?? staticIdleMovement, MovementSlot.Idle);
            }
            else
                StartMovement(staticIdleMovement, MovementSlot.Idle);
        }

        public virtual void UpdateMotion(uint diff)
        {
            if (!_owner)
                return;

            Cypher.Assert(!Empty());

            _cleanFlag |= MotionMasterCleanFlag.Update;
            if (!Top().Update(_owner, diff))
            {
                _cleanFlag &= ~MotionMasterCleanFlag.Update;
                MovementExpired();
            }
            else
                _cleanFlag &= ~MotionMasterCleanFlag.Update;

            if (!_expireList.Empty())
                ClearExpireList();
        }

        public void Clear(bool reset = true)
        {
            if (Convert.ToBoolean(_cleanFlag & MotionMasterCleanFlag.Update))
            {
                if (reset)
                    _cleanFlag |= MotionMasterCleanFlag.Reset;
                else
                    _cleanFlag &= ~MotionMasterCleanFlag.Reset;
                DelayedClean();
            }
            else
                DirectClean(reset);
        }

        public void Clear(MovementSlot slot)
        {
            if (Empty() || IsInvalidMovementSlot(slot))
                return;

            if (_cleanFlag.HasAnyFlag(MotionMasterCleanFlag.Update))
                DelayedClean(slot);
            else
                DirectClean(slot);
        }

        public void MovementExpired(bool reset = true)
        {
            if (Convert.ToBoolean(_cleanFlag & MotionMasterCleanFlag.Update))
            {
                if (reset)
                    _cleanFlag |= MotionMasterCleanFlag.Reset;
                else
                    _cleanFlag &= ~MotionMasterCleanFlag.Reset;
                DelayedExpire();
            }
            else
                DirectExpire(reset);
        }

        public MovementGeneratorType GetCurrentMovementGeneratorType()
        {
            if (Empty())
                return MovementGeneratorType.Max;

            IMovementGenerator movement = Top();
            if (movement == null)
                return MovementGeneratorType.Max;

            return Top().GetMovementGeneratorType();
        }

        public MovementGeneratorType GetMotionSlotType(MovementSlot slot)
        {
            if (Empty() || IsInvalidMovementSlot(slot) || _slot[(int)slot] == null)
                return MovementGeneratorType.Max;

            return _slot[(int)slot].GetMovementGeneratorType();
        }

        public IMovementGenerator GetMotionSlot(MovementSlot slot)
        {
            if (Empty() || IsInvalidMovementSlot(slot) || _slot[(int)slot] == null)
                return null;

            return _slot[(int)slot];
        }

        public IMovementGenerator GetMotionSlot(int slot)
        {
            Cypher.Assert(slot >= 0);
            return _slot[slot];
        }

        public void PropagateSpeedChange()
        {
            if (Empty())
                return;

            IMovementGenerator movement = Top();
            if (movement == null)
                return;

            movement.UnitSpeedChanged();
        }

        public bool GetDestination(out float x, out float y, out float z)
        {
            x = 0f;
            y = 0f;
            z = 0f;
            if (_owner.MoveSpline.Finalized())
                return false;

            Vector3 dest = _owner.MoveSpline.FinalDestination();
            x = dest.X;
            y = dest.Y;
            z = dest.Z;
            return true;
        }

        public void MoveIdle()
        {
            if (Empty() || !IsStatic(Top()))
                StartMovement(staticIdleMovement, MovementSlot.Idle);
        }

        public void MoveTargetedHome()
        {
            Creature owner = _owner.ToCreature();
            if (owner == null)
            {
                Log.outError(LogFilter.Movement, $"MotionMaster::MoveTargetedHome: '{_owner.GetGUID()}', attempted to move towards target home.");
                return;
            }

            Clear(false);

            Unit target = owner.GetCharmerOrOwner();
            if (target == null)
                StartMovement(new HomeMovementGenerator<Creature>(), MovementSlot.Active);
            else
                StartMovement(new FollowMovementGenerator(target, SharedConst.PetFollowDist, new ChaseAngle(SharedConst.PetFollowAngle)), MovementSlot.Active);
        }

        public void MoveRandom(float spawndist = 0.0f)
        {
            if (_owner.IsTypeId(TypeId.Unit))
                StartMovement(new RandomMovementGenerator(spawndist), MovementSlot.Idle);
        }

        public void MoveFollow(Unit target, float dist, float angle = 0.0f, MovementSlot slot = MovementSlot.Idle) { MoveFollow(target, dist, new ChaseAngle(angle), slot); }

        public void MoveFollow(Unit target, float dist, ChaseAngle angle, MovementSlot slot = MovementSlot.Idle)
        {
            // ignore movement request if target not exist
            if (!target || target == _owner)
                return;

            StartMovement(new FollowMovementGenerator(target, dist, angle), slot);
        }

        public void MoveChase(Unit target, float dist, float angle = 0.0f) { MoveChase(target, new ChaseRange(dist), new ChaseAngle(angle)); }

        public void MoveChase(Unit target, ChaseRange? dist = null, ChaseAngle? angle = null)
        {
            if (!target || target == _owner)
                return;

            StartMovement(new ChaseMovementGenerator(target, dist, angle), MovementSlot.Active);
        }

        public void MoveConfused()
        {
            if (_owner.IsTypeId(TypeId.Player))
                StartMovement(new ConfusedGenerator<Player>(), MovementSlot.Controlled);
            else
                StartMovement(new ConfusedGenerator<Creature>(), MovementSlot.Controlled);
        }

        public void MoveFleeing(Unit enemy, uint time)
        {
            if (!enemy)
                return;

            if (_owner.IsCreature())
            {
                if (time != 0)
                    StartMovement(new TimedFleeingGenerator(enemy.GetGUID(), time), MovementSlot.Controlled);
                else
                    StartMovement(new FleeingGenerator<Creature>(enemy.GetGUID()), MovementSlot.Controlled);
            }
            else
                StartMovement(new FleeingGenerator<Player>(enemy.GetGUID()), MovementSlot.Controlled);
        }

        public void MovePoint(uint id, Position pos, bool generatePath = true, float? finalOrient = null)
        {
            MovePoint(id, pos.posX, pos.posY, pos.posZ, generatePath, finalOrient);
        }

        public void MovePoint(uint id, float x, float y, float z, bool generatePath = true, float? finalOrient = null)
        {
            if (_owner.IsTypeId(TypeId.Player))
                StartMovement(new PointMovementGenerator<Player>(id, x, y, z, generatePath, 0.0f, null, null, finalOrient), MovementSlot.Active);
            else
                StartMovement(new PointMovementGenerator<Creature>(id, x, y, z, generatePath, 0.0f, null, null, finalOrient), MovementSlot.Active);
        }

        public void MoveCloserAndStop(uint id, Unit target, float distance)
        {
            float distanceToTravel = _owner.GetExactDist2d(target) - distance;
            if (distanceToTravel > 0.0f)
            {
                float angle = _owner.GetAbsoluteAngle(target);
                float destx = _owner.GetPositionX() + distanceToTravel * (float)Math.Cos(angle);
                float desty = _owner.GetPositionY() + distanceToTravel * (float)Math.Sin(angle);
                MovePoint(id, destx, desty, target.GetPositionZ());
            }
            else
            {
                // we are already close enough. We just need to turn toward the target without changing position.
                MoveSplineInit init = new(_owner);
                init.MoveTo(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ());
                init.SetFacing(target);
                StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, id), MovementSlot.Active);
            }
        }

        public void MoveLand(uint id, Position pos)
        {
            MoveSplineInit init = new(_owner);
            init.MoveTo(pos);
            init.SetAnimation(AnimType.ToGround);
            StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, id), MovementSlot.Active);
        }

        public void MoveTakeoff(uint id, Position pos)
        {
            MoveSplineInit init = new(_owner);
            init.MoveTo(pos);
            init.SetAnimation(AnimType.ToFly);

            StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, id), MovementSlot.Active);
        }

        public void MoveCharge(float x, float y, float z, float speed = SPEED_CHARGE, uint id = EventId.Charge, bool generatePath = false, Unit target = null, SpellEffectExtraData spellEffectExtraData = null)
        {
            if (_slot[(int)MovementSlot.Controlled] != null && _slot[(int)MovementSlot.Controlled].GetMovementGeneratorType() != MovementGeneratorType.Distract)
                return;

            if (_owner.IsTypeId(TypeId.Player))
                StartMovement(new PointMovementGenerator<Player>(id, x, y, z, generatePath, speed, target, spellEffectExtraData), MovementSlot.Controlled);
            else
                StartMovement(new PointMovementGenerator<Creature>(id, x, y, z, generatePath, speed, target, spellEffectExtraData), MovementSlot.Controlled);
        }

        public void MoveCharge(PathGenerator path, float speed = SPEED_CHARGE, Unit target = null, SpellEffectExtraData spellEffectExtraData = null)
        {
            Vector3 dest = path.GetActualEndPosition();

            MoveCharge(dest.X, dest.Y, dest.Z, SPEED_CHARGE, EventId.ChargePrepath);

            // Charge movement is not started when using EVENT_CHARGE_PREPATH
            MoveSplineInit init = new(_owner);
            init.MovebyPath(path.GetPath());
            init.SetVelocity(speed);
            if (target != null)
                init.SetFacing(target);
            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);
            init.Launch();
        }

        public void MoveKnockbackFrom(float srcX, float srcY, float speedXY, float speedZ, SpellEffectExtraData spellEffectExtraData = null)
        {
            //this function may make players fall below map
            if (_owner.IsTypeId(TypeId.Player))
                return;

            if (speedXY < 0.01f)
                return;

            float x, y, z;
            float moveTimeHalf = (float)(speedZ / gravity);
            float dist = 2 * moveTimeHalf * speedXY;
            float max_height = -MoveSpline.ComputeFallElevation(moveTimeHalf, false, -speedZ);

            _owner.GetNearPoint(_owner, out x, out y, out z, dist, _owner.GetAbsoluteAngle(srcX, srcY) + MathFunctions.PI);

            MoveSplineInit init = new(_owner);
            init.MoveTo(x, y, z);
            init.SetParabolic(max_height, 0);
            init.SetOrientationFixed(true);
            init.SetVelocity(speedXY);
            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);

            StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, 0), MovementSlot.Controlled);
        }

        public void MoveJumpTo(float angle, float speedXY, float speedZ)
        {
            //this function may make players fall below map
            if (_owner.IsTypeId(TypeId.Player))
                return;

            float moveTimeHalf = (float)(speedZ / gravity);
            float dist = 2 * moveTimeHalf * speedXY;
            _owner.GetNearPoint2D(null, out float x, out float y, dist, _owner.GetOrientation() + angle);
            float z = _owner.GetPositionZ();
            _owner.UpdateAllowedPositionZ(x, y, ref z);
            MoveJump(x, y, z, 0.0f, speedXY, speedZ);
        }

        public void MoveJump(Position pos, float speedXY, float speedZ, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
        {
            MoveJump(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation(), speedXY, speedZ, id, hasOrientation, arrivalCast, spellEffectExtraData);
        }

        public void MoveJump(float x, float y, float z, float o, float speedXY, float speedZ, uint id = EventId.Jump, bool hasOrientation = false,
            JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
        {
            Log.outDebug(LogFilter.Server, "Unit ({0}) jump to point (X: {1} Y: {2} Z: {3})", _owner.GetGUID().ToString(), x, y, z);
            if (speedXY < 0.01f)
                return;

            float moveTimeHalf = (float)(speedZ / gravity);
            float max_height = -MoveSpline.ComputeFallElevation(moveTimeHalf, false, -speedZ);

            MoveSplineInit init = new(_owner);
            init.MoveTo(x, y, z, false);
            init.SetParabolic(max_height, 0);
            init.SetVelocity(speedXY);
            if (hasOrientation)
                init.SetFacing(o);
            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);

            uint arrivalSpellId = 0;
            ObjectGuid arrivalSpellTargetGuid = ObjectGuid.Empty;
            if (arrivalCast != null)
            {
                arrivalSpellId = arrivalCast.SpellId;
                arrivalSpellTargetGuid = arrivalCast.Target;
            }

            StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, id, arrivalSpellId, arrivalSpellTargetGuid), MovementSlot.Controlled);
        }

        public void MoveCirclePath(float x, float y, float z, float radius, bool clockwise, byte stepCount)
        {
            float step = 2 * MathFunctions.PI / stepCount * (clockwise ? -1.0f : 1.0f);
            Position pos = new(x, y, z, 0.0f);
            float angle = pos.GetAbsoluteAngle(_owner.GetPositionX(), _owner.GetPositionY());

            MoveSplineInit init = new(_owner);
            init.args.path = new Vector3[stepCount];
            for (byte i = 0; i < stepCount; angle += step, ++i)
            {
                Vector3 point = new();
                point.X = (float)(x + radius * Math.Cos(angle));
                point.Y = (float)(y + radius * Math.Sin(angle));

                if (_owner.IsFlying())
                    point.Z = z;
                else
                    point.Z = _owner.GetMapHeight(point.X, point.Y, z) + _owner.GetHoverOffset();

                init.args.path[i] = point;
            }

            if (_owner.IsFlying())
            {
                init.SetFly();
                init.SetCyclic();
                init.SetAnimation(AnimType.ToFly);
            }
            else
            {
                init.SetWalk(true);
                init.SetCyclic();
            }

            StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, 0), MovementSlot.Active);
        }

        void MoveSmoothPath(uint pointId, Vector3[] pathPoints, int pathSize, bool walk = false, bool fly = false)
        {
            MoveSplineInit init = new(_owner);
            if (fly)
            {
                init.SetFly();
                init.SetUncompressed();
                init.SetSmooth();
            }

            init.MovebyPath(pathPoints);
            init.SetWalk(walk);

            // This code is not correct
            // GenericMovementGenerator does not affect UNIT_STATE_ROAMING | UNIT_STATE_ROAMING_MOVE
            // need to call PointMovementGenerator with various pointIds
            StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, pointId), MovementSlot.Active);
        }

        public void MoveAlongSplineChain(uint pointId, uint dbChainId, bool walk)
        {
            Creature owner = _owner.ToCreature();
            if (!owner)
            {
                Log.outError(LogFilter.Misc, "MotionMaster.MoveAlongSplineChain: non-creature {0} tried to walk along DB spline chain. Ignoring.", _owner.GetGUID().ToString());
                return;
            }
            List<SplineChainLink> chain = Global.ScriptMgr.GetSplineChain(owner, (byte)dbChainId);
            if (chain.Empty())
            {
                Log.outError(LogFilter.Misc, "MotionMaster.MoveAlongSplineChain: creature with entry {0} tried to walk along non-existing spline chain with DB id {1}.", owner.GetEntry(), dbChainId);
                return;
            }
            MoveAlongSplineChain(pointId, chain, walk);
        }

        void MoveAlongSplineChain(uint pointId, List<SplineChainLink> chain, bool walk)
        {
            StartMovement(new SplineChainMovementGenerator(pointId, chain, walk), MovementSlot.Active);
        }

        void ResumeSplineChain(SplineChainResumeInfo info)
        {
            if (info.Empty())
            {
                Log.outError(LogFilter.Movement, "MotionMaster.ResumeSplineChain: unit with entry {0} tried to resume a spline chain from empty info.", _owner.GetEntry());
                return;
            }

            StartMovement(new SplineChainMovementGenerator(info), MovementSlot.Active);
        }

        public void MoveFall(uint id = 0)
        {
            // use larger distance for vmap height search than in most other cases
            float tz = _owner.GetMapHeight(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ(), true, MapConst.MaxFallDistance);
            if (tz <= MapConst.InvalidHeight)
                return;

            // Abort too if the ground is very near
            if (Math.Abs(_owner.GetPositionZ() - tz) < 0.1f)
                return;

            _owner.SetFall(true);

            // don't run spline movement for players
            if (_owner.IsTypeId(TypeId.Player))
            {
                _owner.ToPlayer().SetFallInformation(0, _owner.GetPositionZ());
                return;
            }

            MoveSplineInit init = new(_owner);
            init.MoveTo(_owner.GetPositionX(), _owner.GetPositionY(), tz + _owner.GetHoverOffset(), false);
            init.SetFall();
            StartMovement(new GenericMovementGenerator(init, MovementGeneratorType.Effect, id), MovementSlot.Controlled);
        }

        public void MoveSeekAssistance(float x, float y, float z)
        {
            if (_owner.IsCreature())
            {
                _owner.AttackStop();
                _owner.CastStop();
                _owner.ToCreature().SetReactState(ReactStates.Passive);
                StartMovement(new AssistanceMovementGenerator(x, y, z), MovementSlot.Active);
            }
            else
                Log.outError(LogFilter.Server, $"MotionMaster::MoveSeekAssistance: {_owner.GetGUID()}, attempted to seek assistance");
        }

        public void MoveSeekAssistanceDistract(uint time)
        {
            if (_owner.IsCreature())
                StartMovement(new AssistanceDistractMovementGenerator(time), MovementSlot.Active);
            else
                Log.outError(LogFilter.Server, $"MotionMaster::MoveSeekAssistanceDistract: {_owner.GetGUID()} attempted to call distract after assistance");
        }

        public void MoveTaxiFlight(uint path, uint pathnode)
        {
            if (_owner.IsTypeId(TypeId.Player))
            {
                if (path < CliDB.TaxiPathNodesByPath.Count)
                {
                    Log.outDebug(LogFilter.Server, $"MotionMaster::MoveTaxiFlight: {_owner.GetGUID()} taxi to Path Id: {path} (node {pathnode})");
                    FlightPathMovementGenerator movement = new();
                    movement.LoadPath(_owner.ToPlayer());
                    StartMovement(movement, MovementSlot.Controlled);
                }
                else
                    Log.outError(LogFilter.Movement, $"MotionMaster::MoveTaxiFlight: '{_owner.GetGUID()}', attempted taxi to non-existing path Id: {path} (node: {pathnode})");

            }
            else
                Log.outError(LogFilter.Movement, $"MotionMaster::MoveTaxiFlight: '{_owner.GetGUID()}', attempted taxi to path Id: {path} (node: {pathnode})");
        }

        public void MoveDistract(uint timer)
        {
            if (_slot[(int)MovementSlot.Controlled] != null)
                return;

            StartMovement(new DistractMovementGenerator(timer), MovementSlot.Controlled);
        }

        public void MovePath(uint pathId, bool repeatable)
        {
            if (pathId == 0)
                return;

            StartMovement(new WaypointMovementGenerator(pathId, repeatable), MovementSlot.Idle);
        }

        public void MovePath(WaypointPath path, bool repeatable)
        {
            StartMovement(new WaypointMovementGenerator(path, repeatable), MovementSlot.Idle);
        }

        public void MoveRotate(uint time, RotateDirection direction)
        {
            if (time == 0)
                return;

            StartMovement(new RotateMovementGenerator(time, direction), MovementSlot.Active);
        }

        public void MoveFormation(uint id, Position destination, WaypointMoveType moveType, bool forceRun = false, bool forceOrientation = false)
        {
            if (_owner.GetTypeId() == TypeId.Unit)
                StartMovement(new FormationMovementGenerator(id, destination, moveType, forceRun, forceOrientation), MovementSlot.Active);
        }

        public void LaunchMoveSpline(MoveSplineInit init, uint id = 0, MovementSlot slot = MovementSlot.Active, MovementGeneratorType type = MovementGeneratorType.Effect)
        {
            if (IsInvalidMovementGeneratorType(type))
            {
                Log.outDebug(LogFilter.Movement, $"MotionMaster::LaunchMoveSpline: '{_owner.GetGUID()}', tried to launch a spline with an invalid MovementGeneratorType: {type} (Id: {id}, Slot: {slot})");
                return;
            }

            StartMovement(new GenericMovementGenerator(init, type, id), slot);
        }

        void Pop()
        {
            if (Empty())
                return;

            _slot[_top] = null;
            while (!Empty() && Top() == null)
                --_top;
        }

        bool NeedInitTop()
        {
            if (Empty())
                return false;

            return _initialize[_top];
        }

        void InitTop()
        {
            Top().Initialize(_owner);
            _initialize[_top] = false;
        }

        void StartMovement(IMovementGenerator m, MovementSlot slot)
        {
            IMovementGenerator curr = _slot[(int)slot];
            if (curr != null)
            {
                _slot[(int)slot] = null; // in case a new one is generated in this slot during directdelete
                if (_top == (int)slot && Convert.ToBoolean(_cleanFlag & MotionMasterCleanFlag.Update))
                    DelayedDelete(curr);
                else
                    DirectDelete(curr);
            }
            else if (_top < (int)slot)
            {
                _top = (int)slot;
            }

            _slot[(int)slot] = m;
            if (_top > (int)slot)
                _initialize[(int)slot] = true;
            else
            {
                _initialize[(int)slot] = false;
                m.Initialize(_owner);
            }
        }

        void DirectClean(bool reset)
        {
            while (Size() > 1)
            {
                IMovementGenerator curr = Top();
                Pop();
                if (curr != null)
                    DirectDelete(curr);
            }

            if (Empty())
                return;

            if (NeedInitTop())
                InitTop();
            else if (reset)
                Top().Reset(_owner);
        }

        void DirectClean(MovementSlot slot)
        {
            IMovementGenerator motion = GetMotionSlot(slot);
            if (motion != null)
            {
                _slot[(int)slot] = null;
                DirectDelete(motion);
            }

            while (!Empty() && Top() == null)
                --_top;

            if (Empty())
                Initialize();
            else if (NeedInitTop())
                InitTop();
        }

        void DelayedClean()
        {
            while (Size() > 1)
            {
                IMovementGenerator curr = Top();
                Pop();
                if (curr != null)
                    DelayedDelete(curr);
            }
        }

        void DelayedClean(MovementSlot slot)
        {
            IMovementGenerator motion = GetMotionSlot(slot);
            if (motion != null)
            {
                _slot[(int)slot] = null;
                DelayedDelete(motion);
            }

            while (!Empty() && Top() == null)
                --_top;
        }

        void DirectExpire(bool reset)
        {
            if (Size() > 1)
            {
                IMovementGenerator curr = Top();
                Pop();
                DirectDelete(curr);
            }

            while (!Empty() && Top() == null)//not sure this will work
                --_top;

            if (Empty())
                Initialize();
            else if (NeedInitTop())
                InitTop();
            else if (reset)
                Top().Reset(_owner);
        }

        void DelayedExpire()
        {
            if (Size() > 1)
            {
                IMovementGenerator curr = Top();
                Pop();
                DelayedDelete(curr);
            }

            while (!Empty() && Top() == null)
                --_top;
        }

        void DirectDelete(IMovementGenerator curr)
        {
            if (IsStatic(curr))
                return;
            curr.Finalize(_owner);
        }

        void DelayedDelete(IMovementGenerator curr)
        {
            if (IsStatic(curr))
                return;

            _expireList.Add(curr);
        }

        void ClearExpireList()
        {
            for (int i = 0; i < _expireList.Count; ++i)
                DirectDelete(_expireList[i]);

            _expireList.Clear();

            if (Empty())
                Initialize();
            else if (NeedInitTop())
                InitTop();
            else if (_cleanFlag.HasAnyFlag(MotionMasterCleanFlag.Reset))
                Top().Reset(_owner);

            _cleanFlag &= ~MotionMasterCleanFlag.Reset;
        }

        public bool Empty() { return (_top < 0); }

        int Size() { return _top + 1; }

        public static uint SplineId
        {
            get { return splineId++; }
        }

        bool IsStatic(IMovementGenerator movement)
        {
            return (movement == staticIdleMovement);
        }

        public static bool IsInvalidMovementGeneratorType(MovementGeneratorType type) { return type == MovementGeneratorType.MaxDB || type >= MovementGeneratorType.Max; }
        public static bool IsInvalidMovementSlot(MovementSlot slot) { return slot >= MovementSlot.Max; }

        static uint splineId;

        Unit _owner { get; }
        IMovementGenerator[] _slot = new IMovementGenerator[(int)MovementSlot.Max];
        MotionMasterCleanFlag _cleanFlag;
        bool[] _initialize = new bool[(int)MovementSlot.Max];
        int _top;
        List<IMovementGenerator> _expireList = new();
    }

    public class JumpArrivalCastArgs
    {
        public uint SpellId;
        public ObjectGuid Target;
    }

    enum MotionMasterCleanFlag
    {
        None = 0,
        Update = 1, // Clear or Expire called from update
        Reset = 2  // Flag if need top().Reset()
    }

    public struct ChaseRange
    {
        // this contains info that informs how we should path!
        public float MinRange;     // we have to move if we are within this range...    (min. attack range)
        public float MinTolerance; // ...and if we are, we will move this far away
        public float MaxRange;     // we have to move if we are outside this range...   (max. attack range)
        public float MaxTolerance; // ...and if we are, we will move into this range

        public ChaseRange(float range)
        {
            MinRange = range > SharedConst.ContactDistance ? 0 : range - SharedConst.ContactDistance;
            MinTolerance = range;
            MaxRange = range + SharedConst.ContactDistance;
            MaxTolerance = range;
        }

        public ChaseRange(float min, float max)
        {
            MinRange = min;
            MinTolerance = Math.Min(min + SharedConst.ContactDistance, (min + max) / 2);
            MaxRange = max;
            MaxTolerance = Math.Max(max - SharedConst.ContactDistance, MinTolerance);
        }

        public ChaseRange(float min, float tMin, float tMax, float max)
        {
            MinRange = min;
            MinTolerance = tMin;
            MaxRange = max;
            MaxTolerance = tMax;
        }
    }

    public struct ChaseAngle
    {
        public float RelativeAngle; // we want to be at this angle relative to the target (0 = front, M_PI = back)
        public float Tolerance;     // but we'll tolerate anything within +- this much

        public ChaseAngle(float angle, float tol = MathFunctions.PiOver4)
        {
            RelativeAngle = Position.NormalizeOrientation(angle);
            Tolerance = tol;
        }

        public float UpperBound() { return Position.NormalizeOrientation(RelativeAngle + Tolerance); }

        public float LowerBound() { return Position.NormalizeOrientation(RelativeAngle - Tolerance); }

        public bool IsAngleOkay(float relAngle)
        {
            float diff = Math.Abs(relAngle - RelativeAngle);
            return (Math.Min(diff, (2 * MathF.PI) - diff) <= Tolerance);
        }
    }
}
