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
        public const float SPEED_CHARGE  =  42.0f;
        IdleMovementGenerator staticIdleMovement = new IdleMovementGenerator();

        public MotionMaster(Unit me)
        {
            _owner = me;
            _expireList = null;
            _top = -1;
            _cleanFlag = MMCleanFlag.None;

            for (byte i = 0; i < (int)MovementSlot.Max; ++i)
            {
                _slot[i] = null;
                _initialize[i] = true;
            }
        }

        public void Initialize()
        {
            while (!empty())
            {
                IMovementGenerator curr = top();
                pop();
                if (curr != null)
                    DirectDelete(curr);
            }

            InitDefault();
        }

        public void InitDefault()
        {
            if (_owner.IsTypeId(TypeId.Unit))
            {
                IMovementGenerator movement = AISelector.SelectMovementAI(_owner.ToCreature());
                StartMovement(movement ?? staticIdleMovement, MovementSlot.Idle);
            }
            else
                StartMovement(staticIdleMovement, MovementSlot.Idle);
        }

        public virtual void UpdateMotion(uint diff)
        {
            if (!_owner)
                return;

            if (_owner.HasUnitState(UnitState.Root | UnitState.Stunned))
                return;

            Cypher.Assert(!empty());

            _cleanFlag |= MMCleanFlag.Update;
            bool isMoveGenUpdateSuccess = top().Update(_owner, diff);
            _cleanFlag &= ~MMCleanFlag.Update;

            if (!isMoveGenUpdateSuccess)
                MovementExpired();

            if (_expireList != null)
                ClearExpireList();
        }

        public void Clear(bool reset = true)
        {
            if (Convert.ToBoolean(_cleanFlag & MMCleanFlag.Update))
            {
                if (reset)
                    _cleanFlag |= MMCleanFlag.Reset;
                else
                    _cleanFlag &= ~MMCleanFlag.Reset;
                DelayedClean();
            }
            else
                DirectClean(reset);
        }

        void ClearExpireList()
        {
            for (int i = 0; i < _expireList.Count; ++i)
            {
                IMovementGenerator mg = _expireList[i];
                DirectDelete(mg);
            }

            _expireList = null;

            if (empty())
                Initialize();
            else if (NeedInitTop())
                InitTop();
            else if (Convert.ToBoolean(_cleanFlag & MMCleanFlag.Reset))
                top().Reset(_owner);

            _cleanFlag &= ~MMCleanFlag.Reset;
        }

        public void MovementExpired(bool reset = true)
        {
            if (Convert.ToBoolean(_cleanFlag & MMCleanFlag.Update))
            {
                if (reset)
                    _cleanFlag |= MMCleanFlag.Reset;
                else
                    _cleanFlag &= ~MMCleanFlag.Reset;
                DelayedExpire();
            }
            else
                DirectExpire(reset);
        }

        public MovementGeneratorType GetCurrentMovementGeneratorType()
        {
            if (empty())
                return MovementGeneratorType.Idle;

            return top().GetMovementGeneratorType();
        }

        public MovementGeneratorType GetMotionSlotType(MovementSlot slot)
        {
            if (_slot[(int)slot] == null)
                return MovementGeneratorType.Null;
            else
                return _slot[(int)slot].GetMovementGeneratorType();
        }

        public IMovementGenerator GetMotionSlot(int slot)
        {
            Cypher.Assert(slot >= 0);
            return _slot[slot];
        }

        public void propagateSpeedChange()
        {
            for (int i = 0; i <= _top; ++i)
            {
                if (_slot[i] != null)
                    _slot[i].unitSpeedChanged();
            }
        }

        public bool GetDestination(out float x, out float y, out float z)
        {
            x = 0f;
            y = 0f;
            z = 0f;
            if (_owner.moveSpline.Finalized())
                return false;

            Vector3 dest = _owner.moveSpline.FinalDestination();
            x = dest.X;
            y = dest.Y;
            z = dest.Z;
            return true;
        }

        public void MoveIdle()
        {
            if (empty() || !IsStatic(top()))
                StartMovement(staticIdleMovement, MovementSlot.Idle);
        }

        public void MoveTargetedHome()
        {
            Clear(false);

            if (_owner.IsTypeId(TypeId.Unit) && _owner.ToCreature().GetCharmerOrOwnerGUID().IsEmpty())
            {
                StartMovement(new HomeMovementGenerator<Creature>(), MovementSlot.Active);
            }
            else if (_owner.IsTypeId(TypeId.Unit) && !_owner.ToCreature().GetCharmerOrOwnerGUID().IsEmpty())
            {
                Unit target = _owner.ToCreature().GetCharmerOrOwner();
                if (target)
                    StartMovement(new FollowMovementGenerator<Creature>(target, SharedConst.PetFollowDist, SharedConst.PetFollowAngle), MovementSlot.Active);
            }
        }

        public void MoveRandom(float spawndist = 0.0f)
        {
            if (_owner.IsTypeId(TypeId.Unit))
                StartMovement(new RandomMovementGenerator(spawndist), MovementSlot.Idle);
        }

        public void MoveFollow(Unit target, float dist = 0.0f, float angle = 0.0f, MovementSlot slot = MovementSlot.Idle)
        {
            // ignore movement request if target not exist
            if (!target || target == _owner)
                return;

            if (_owner.IsTypeId(TypeId.Player))
                StartMovement(new FollowMovementGenerator<Player>(target, dist, angle), slot);
            else
                StartMovement(new FollowMovementGenerator<Creature>(target, dist, angle), slot);
        }

        public void MoveChase(Unit target, float dist = 0.0f, float angle = 0.0f)
        {
            if (!target || target == _owner)
                return;

            if (_owner.IsTypeId(TypeId.Player))
                StartMovement(new ChaseMovementGenerator<Player>(target, dist, angle), MovementSlot.Active);
            else
                StartMovement(new ChaseMovementGenerator<Creature>(target, dist, angle), MovementSlot.Active);
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

            if (_owner.IsTypeId(TypeId.Player))
                StartMovement(new FleeingGenerator<Player>(enemy.GetGUID()), MovementSlot.Controlled);
            else
            {
                if (time != 0)
                    StartMovement(new TimedFleeingGenerator(enemy.GetGUID(), time), MovementSlot.Controlled);
                else
                    StartMovement(new FleeingGenerator<Creature>(enemy.GetGUID()), MovementSlot.Controlled);
            }
        }

        public void MovePoint(uint id, Position pos, bool generatePath = true)
        {
            MovePoint(id, pos.posX, pos.posY, pos.posZ, generatePath);
        }

        public void MovePoint(ulong id, float x, float y, float z, bool generatePath = true)
        {
            if (_owner.IsTypeId(TypeId.Player))
                StartMovement(new PointMovementGenerator<Player>(id, x, y, z, generatePath), MovementSlot.Active);
            else
                StartMovement(new PointMovementGenerator<Creature>(id, x, y, z, generatePath), MovementSlot.Active);
        }

        void MoveCloserAndStop(uint id, Unit target, float distance)
        {
            float distanceToTravel = _owner.GetExactDist2d(target) - distance;
            if (distanceToTravel > 0.0f)
            {
                float angle = _owner.GetAngle(target);
                float destx = _owner.GetPositionX() + distanceToTravel * (float)Math.Cos(angle);
                float desty = _owner.GetPositionY() + distanceToTravel * (float)Math.Sin(angle);
                MovePoint(id, destx, desty, target.GetPositionZ());
            }
            else
            {
                // we are already close enough. We just need to turn toward the target without changing position.
                MoveSplineInit init = new MoveSplineInit(_owner);
                init.MoveTo(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZMinusOffset());
                init.SetFacing(target);
                init.Launch();
                StartMovement(new EffectMovementGenerator(id), MovementSlot.Active);
            }
        }

        public void MoveLand(uint id, Position pos)
        {
            float x, y, z;
            pos.GetPosition(out x, out y, out z);

            MoveSplineInit init = new MoveSplineInit(_owner);
            init.MoveTo(x, y, z);
            init.SetAnimation(AnimType.ToGround);
            init.Launch();
            StartMovement(new EffectMovementGenerator(id), MovementSlot.Active);
        }

        void MoveTakeoff(uint id, Position pos)
        {
            float x, y, z;
            pos.GetPosition(out x, out y, out z);

            MoveSplineInit init = new MoveSplineInit(_owner);
            init.MoveTo(x, y, z);
            init.SetAnimation(AnimType.ToFly);
            init.Launch();
            StartMovement(new EffectMovementGenerator(id), MovementSlot.Active);
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
            MoveSplineInit init = new MoveSplineInit(_owner);
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

            float x, y, z;
            float moveTimeHalf = (float)(speedZ / gravity);
            float dist = 2 * moveTimeHalf * speedXY;
            float max_height = -MoveSpline.computeFallElevation(moveTimeHalf, false, -speedZ);

            _owner.GetNearPoint(_owner, out x, out y, out z, _owner.GetObjectSize(), dist, _owner.GetAngle(srcX, srcY) + MathFunctions.PI);

            MoveSplineInit init = new MoveSplineInit(_owner);
            init.MoveTo(x, y, z);
            init.SetParabolic(max_height, 0);
            init.SetOrientationFixed(true);
            init.SetVelocity(speedXY);
            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);

            init.Launch();
            StartMovement(new EffectMovementGenerator(0), MovementSlot.Controlled);
        }

        public void MoveJumpTo(float angle, float speedXY, float speedZ)
        {
            //this function may make players fall below map
            if (_owner.IsTypeId(TypeId.Player))
                return;

            float x, y, z;

            float moveTimeHalf = (float)(speedZ / gravity);
            float dist = 2 * moveTimeHalf * speedXY;
            _owner.GetClosePoint(out x, out y, out z, _owner.GetObjectSize(), dist, angle);
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

            float moveTimeHalf = (float)(speedZ / gravity);
            float max_height = -MoveSpline.computeFallElevation(moveTimeHalf, false, -speedZ);

            MoveSplineInit init = new MoveSplineInit(_owner);
            init.MoveTo(x, y, z, false);
            init.SetParabolic(max_height, 0);
            init.SetVelocity(speedXY);
            if (hasOrientation)
                init.SetFacing(o);
            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);
            init.Launch();

            uint arrivalSpellId = 0;
            ObjectGuid arrivalSpellTargetGuid = ObjectGuid.Empty;
            if (arrivalCast != null)
            {
                arrivalSpellId = arrivalCast.SpellId;
                arrivalSpellTargetGuid = arrivalCast.Target;
            }

            StartMovement(new EffectMovementGenerator(id, arrivalSpellId, arrivalSpellTargetGuid), MovementSlot.Controlled);
        }

        void MoveCirclePath(float x, float y, float z, float radius, bool clockwise, byte stepCount)
        {
            float step = 2 * MathFunctions.PI / stepCount * (clockwise ? -1.0f : 1.0f);
            Position pos = new Position(x, y, z, 0.0f);
            float angle = pos.GetAngle(_owner.GetPositionX(), _owner.GetPositionY());

            MoveSplineInit init = new MoveSplineInit(_owner);
            init.args.path = new Vector3[stepCount];
            for (byte i = 0; i < stepCount; angle += step, ++i)
            {
                Vector3 point = new Vector3();
                point.X = (float)(x + radius * Math.Cos(angle));
                point.Y = (float)(y + radius * Math.Sin(angle));

                if (_owner.IsFlying())
                    point.Z = z;
                else
                    point.Z = _owner.GetMap().GetHeight(_owner.GetPhaseShift(), point.X, point.Y, z);

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

            init.Launch();
        }

        void MoveSmoothPath(uint pointId, Vector3[] pathPoints, int pathSize, bool walk = false, bool fly = false)
        {
            MoveSplineInit init = new MoveSplineInit(_owner);
            if (fly)
            {
                init.SetFly();
                init.SetUncompressed();
                init.SetSmooth();
            }

            init.MovebyPath(pathPoints);
            init.SetWalk(walk);
            init.Launch();

            // This code is not correct
            // EffectMovementGenerator does not affect UNIT_STATE_ROAMING | UNIT_STATE_ROAMING_MOVE
            // need to call PointMovementGenerator with various pointIds
            StartMovement(new EffectMovementGenerator(pointId), MovementSlot.Active);
            //Position pos(pathPoints[pathSize - 1].x, pathPoints[pathSize - 1].y, pathPoints[pathSize - 1].z);
            //MovePoint(EVENT_CHARGE_PREPATH, pos, false);
        }

        void MoveAlongSplineChain(uint pointId, uint dbChainId, bool walk)
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
            float tz = _owner.GetMap().GetHeight(_owner.GetPhaseShift(), _owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ(), true, MapConst.MaxFallDistance);
            if (tz <= MapConst.InvalidHeight)
                return;

            // Abort too if the ground is very near
            if (Math.Abs(_owner.GetPositionZ() - tz) < 0.1f)
                return;

            _owner.SetFall(true);

            // don't run spline movement for players
            if (_owner.IsTypeId(TypeId.Player))
                return;

            MoveSplineInit init = new MoveSplineInit(_owner);
            init.MoveTo(_owner.GetPositionX(), _owner.GetPositionY(), tz, false);
            init.SetFall();
            init.Launch();
            StartMovement(new EffectMovementGenerator(id), MovementSlot.Controlled);
        }

        public void MoveSeekAssistance(float x, float y, float z)
        {
            if (_owner.IsTypeId(TypeId.Player))
                Log.outError(LogFilter.Server, "Player {0} attempt to seek assistance", _owner.GetGUID().ToString());
            else
            {
                _owner.AttackStop();
                _owner.CastStop();
                _owner.ToCreature().SetReactState(ReactStates.Passive);
                StartMovement(new AssistanceMovementGenerator(x, y, z), MovementSlot.Active);
            }
        }

        public void MoveSeekAssistanceDistract(uint time)
        {
            if (_owner.IsTypeId(TypeId.Player))
                Log.outError(LogFilter.Server, "Player {0} attempt to call distract after assistance", _owner.GetGUID().ToString());
            else
                StartMovement(new AssistanceDistractMovementGenerator(time), MovementSlot.Active);
        }

        public void MoveTaxiFlight(uint path, uint pathnode)
        {
            if (_owner.IsTypeId(TypeId.Player))
            {
                if (path < CliDB.TaxiPathNodesByPath.Count)
                {
                    Log.outDebug(LogFilter.Server, "{0} taxi to (Path {1} node {2})", _owner.GetName(), path, pathnode);
                    FlightPathMovementGenerator mgen = new FlightPathMovementGenerator();
                    mgen.LoadPath(_owner.ToPlayer());
                    StartMovement(mgen, MovementSlot.Controlled);
                }
                else
                {
                    Log.outDebug(LogFilter.Server, "{0} attempt taxi to (not existed Path {1} node {2})",
                    _owner.GetName(), path, pathnode);
                }
            }
            else
            {
                Log.outDebug(LogFilter.Server, "Creature (Entry: {0} GUID: {1}) attempt taxi to (Path {2} node {3})",
                _owner.GetEntry(), _owner.GetGUID().ToString(), path, pathnode);
            }
        }

        public void MoveDistract(uint timer)
        {
            if (_slot[(int)MovementSlot.Controlled] != null)
                return;

            StartMovement(new DistractMovementGenerator(timer), MovementSlot.Controlled);
        }

        public void MovePath(uint path_id, bool repeatable)
        {
            if (path_id == 0)
                return;

            StartMovement(new WaypointMovementGenerator(path_id, repeatable), MovementSlot.Idle);
        }

        public void MovePath(WaypointPath path, bool repeatable)
        {
            StartMovement(new WaypointMovementGenerator(path, repeatable), MovementSlot.Idle);
        }

        void MoveRotate(uint time, RotateDirection direction)
        {
            if (time == 0)
                return;

            StartMovement(new RotateMovementGenerator(time, direction), MovementSlot.Active);
        }

        void pop()
        {
            if (empty())
                return;

            _slot[_top] = null;
            while (!empty() && top() == null)
                --_top;
        }

        bool NeedInitTop()
        {
            if (empty())
                return false;

            return _initialize[_top];
        }

        void InitTop()
        {
            top().Initialize(_owner);
            _initialize[_top] = false;
        }

        void StartMovement(IMovementGenerator m, MovementSlot slot)
        {
            IMovementGenerator curr = _slot[(int)slot];
            if (curr != null)
            {
                _slot[(int)slot] = null; // in case a new one is generated in this slot during directdelete
                if (_top == (int)slot && Convert.ToBoolean(_cleanFlag & MMCleanFlag.Update))
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
            while (size() > 1)
            {
                IMovementGenerator curr = top();
                pop();
                if (curr != null)
                    DirectDelete(curr);
            }

            if (empty())
                return;

            if (NeedInitTop())
                InitTop();
            else if (reset)
                top().Reset(_owner);
        }

        void DelayedClean()
        {
            while (size() > 1)
            {
                IMovementGenerator curr = top();
                pop();
                if (curr != null)
                    DelayedDelete(curr);
            }
        }

        void DirectExpire(bool reset)
        {
            if (size() > 1)
            {
                IMovementGenerator curr = top();
                pop();
                DirectDelete(curr);
            }

            while (!empty() && top() == null)//not sure this will work
                --_top;

            if (empty())
                Initialize();
            else if (NeedInitTop())
                InitTop();
            else if (reset)
                top().Reset(_owner);
        }

        void DelayedExpire()
        {
            if (size() > 1)
            {
                IMovementGenerator curr = top();
                pop();
                DelayedDelete(curr);
            }

            while (!empty() && top() == null)
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
            if (_expireList == null)
                _expireList = new List<IMovementGenerator>();
            _expireList.Add(curr);
        }

        public bool empty() { return (_top < 0); }

        int size() { return _top + 1; }

        public IMovementGenerator top()
        {
            Cypher.Assert(!empty());
            return _slot[_top];
        }

        public static uint SplineId
        {
            get { return splineId++; }
        }

        bool IsStatic(IMovementGenerator movement)
        {
            return (movement == staticIdleMovement);
        }

        static uint splineId;

        Unit _owner { get; }
        IMovementGenerator[] _slot = new IMovementGenerator[(int)MovementSlot.Max];
        MMCleanFlag _cleanFlag;
        bool[] _initialize = new bool[(int)MovementSlot.Max];
        int _top;
        List<IMovementGenerator> _expireList = new List<IMovementGenerator>();
    }

    public class JumpArrivalCastArgs
    {
        public uint SpellId;
        public ObjectGuid Target;
    }

    enum MMCleanFlag
    {
        None = 0,
        Update = 1, // Clear or Expire called from update
        Reset = 2  // Flag if need top().Reset()
    }
}
