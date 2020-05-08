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
using Game.Entities;
using System;

namespace Game.Movement
{
    public interface ITargetedMovementGeneratorBase
    {
        bool IsTargetValid();
        Unit GetTarget();
        void StopFollowing();
    }

    public abstract class TargetedMovementGenerator<T> : MovementGeneratorMedium<T>, ITargetedMovementGeneratorBase where T : Unit
    {
        protected TargetedMovementGenerator(Unit target, float offset = 0, float angle = 0)
        {
            _target = new FollowerReference();
            _target.Link(target, this);
            _timer = new TimeTracker();
            _offset = offset;
            _angle = angle;
            _recalculateTravel = false;
            _targetReached = false;
        }

        public override bool DoUpdate(T owner, uint diff)
        {
            if (!IsTargetValid() || !GetTarget().IsInWorld)
                return false;

            if (owner == null || !owner.IsAlive())
                return false;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting() || HasLostTarget(owner))
            {
                _interrupt = true;
                owner.StopMoving();
                return true;
            }

            if (_interrupt || _recalculateTravel)
            {
                _interrupt = false;
                SetTargetLocation(owner, true);
                return true;
            }


            bool targetMoved = false;
            _timer.Update((int)diff);
            if (_timer.Passed())
            {
                _timer.Reset(100);

                float distance = owner.GetCombatReach() + WorldConfig.GetFloatValue(WorldCfg.RateTargetPosRecalculationRange);
                if (owner.IsPet() && (owner.GetCharmerOrOwnerGUID() == GetTarget().GetGUID()))
                    distance = 1.0f; // pet following owner

                Vector3 destination = owner.MoveSpline.FinalDestination();
                if (owner.MoveSpline.onTransport)
                {
                    float o = 0;
                    ITransport transport = owner.GetDirectTransport();
                    if (transport != null)
                        transport.CalculatePassengerPosition(ref destination.X, ref destination.Y, ref destination.Z, ref o);
                }

                // First check distance
                if (owner.IsTypeId(TypeId.Unit) && owner.ToCreature().CanFly())
                    targetMoved = !GetTarget().IsWithinDist3d(destination.X, destination.Y, destination.Z, distance);
                else
                    targetMoved = !GetTarget().IsWithinDist2d(destination.X, destination.Y, distance);


                // then, if the target is in range, check also Line of Sight.
                if (!targetMoved)
                    targetMoved = !GetTarget().IsWithinLOSInMap(owner);
            }

            if (targetMoved)
                SetTargetLocation(owner, true);
            else if (_speedChanged)
                SetTargetLocation(owner, false);

            if (!_targetReached && owner.MoveSpline.Finalized())
            {
                MovementInform(owner);
                if (_angle == 0.0f && !owner.HasInArc(0.01f, GetTarget()))
                    owner.SetInFront(GetTarget());

                if (!_targetReached)
                {
                    _targetReached = true;
                    ReachTarget(owner);
                }
            }

            return true;
        }

        public void SetTargetLocation(T owner, bool updateDestination)
        {
            if (!IsTargetValid() || !GetTarget().IsInWorld)
                return;

            if (!owner || !owner.IsAlive())
                return;


            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting() || HasLostTarget(owner))
            {
                _interrupt = true;
                owner.StopMoving();
                return;
            }

            if (owner.IsTypeId(TypeId.Unit) && !GetTarget().IsInAccessiblePlaceFor(owner.ToCreature()))
            {
                owner.ToCreature().SetCannotReachTarget(true);
                return;
            }

            float x, y, z;
            if (updateDestination || _path == null)
            {
                if (_offset == 0)
                {
                    if (GetTarget().IsWithinDistInMap(owner, SharedConst.ContactDistance))
                        return;

                    // to nearest contact position
                    GetTarget().GetContactPoint(owner, out x, out y, out z);
                }
                else
                {
                    float distance = _offset + 1.0f;
                    float size = owner.GetCombatReach();

                    if (owner.IsPet() && GetTarget().GetTypeId() == TypeId.Player)
                    {
                        distance = 1.0f;
                        size = 1.0f;
                    }

                    if (GetTarget().IsWithinDistInMap(owner, distance))
                        return;

                    GetTarget().GetClosePoint(out x, out y, out z, size, _offset, _angle);
                }
            }
            else
            {
                // the destination has not changed, we just need to refresh the path (usually speed change)
                var end = _path.GetEndPosition();
                x = end.X;
                y = end.Y;
                z = end.Z;
            }

            if (_path == null)
                _path = new PathGenerator(owner);

            // allow pets to use shortcut if no path found when following their master
            bool forceDest = owner.IsTypeId(TypeId.Unit) && owner.IsPet() && owner.HasUnitState(UnitState.Follow);

            bool result = _path.CalculatePath(x, y, z, forceDest);
            if (!result && Convert.ToBoolean(_path.GetPathType() & PathType.NoPath))
            {
                // Can't reach target
                _recalculateTravel = true;
                if (owner.IsTypeId(TypeId.Unit))
                    owner.ToCreature().SetCannotReachTarget(true);
                return;
            }

            _targetReached = false;
            _recalculateTravel = false;
            _speedChanged = false;

            AddUnitStateMove(owner);

            if (owner.IsTypeId(TypeId.Unit))
                owner.ToCreature().SetCannotReachTarget(false);

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MovebyPath(_path.GetPath());
            init.SetWalk(EnableWalking());
            // Using the same condition for facing target as the one that is used for SetInFront on movement end
            // - applies to ChaseMovementGenerator mostly
            if (_angle == 0.0f)
                init.SetFacing(GetTarget());

            init.Launch();
        }

        bool IsReachable()
        {
            return _path != null ? _path.GetPathType().HasAnyFlag(PathType.Normal) : true;
        }

        public override void UnitSpeedChanged()
        {
            _speedChanged = true;
        }

        public abstract void ClearUnitStateMove(T owner);
        public abstract void AddUnitStateMove(T owner);
        public virtual bool HasLostTarget(T owner) { return false; }
        public abstract void ReachTarget(T owner);
        public virtual bool EnableWalking() { return false; }
        public abstract void MovementInform(T owner);
        
        public void StopFollowing() { }

        public bool IsTargetValid() { return _target.IsValid(); }
        public Unit GetTarget() { return _target.GetTarget(); }

        FollowerReference _target;
        PathGenerator _path;
        TimeTracker _timer;
        float _offset;
        float _angle;
        bool _recalculateTravel;
        bool _speedChanged;
        bool _targetReached;
        bool _interrupt;
    }

    public class ChaseMovementGenerator<T> : TargetedMovementGenerator<T> where T : Unit
    {
        public ChaseMovementGenerator(Unit target)
            : base(target)
        {
        }

        public ChaseMovementGenerator(Unit target, float offset, float angle)
            : base(target, offset, angle)
        {
        }

        public override void DoInitialize(T owner)
        {
            if (owner.IsTypeId(TypeId.Unit))
                owner.SetWalk(false);

            owner.AddUnitState(UnitState.Chase);
            SetTargetLocation(owner, true);
        }

        public override void DoReset(T owner)
        {
            DoInitialize(owner);
        }

        public override void DoFinalize(T owner)
        {
            owner.ClearUnitState(UnitState.Chase | UnitState.ChaseMove);
        }

        public override void ClearUnitStateMove(T owner)
        {
            owner.ClearUnitState(UnitState.ChaseMove);
        }

        public override void AddUnitStateMove(T owner)
        {
            owner.AddUnitState(UnitState.ChaseMove);
        }

        public override bool HasLostTarget(T owner)
        {
            return owner.GetVictim() != GetTarget();
        }

        public override void ReachTarget(T owner)
        {
            ClearUnitStateMove(owner);

            if (owner.IsWithinMeleeRange(GetTarget()))
                owner.Attack(GetTarget(), true);

            if (owner.IsTypeId(TypeId.Unit))
                owner.ToCreature().SetCannotReachTarget(false);
        }

        public override void MovementInform(T owner)
        {
            if (owner.IsTypeId(TypeId.Unit))
            {
                // Pass back the GUIDLow of the target. If it is pet's owner then PetAI will handle
                if (owner.ToCreature().GetAI() != null)
                    owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Chase, (uint)GetTarget().GetGUID().GetCounter());
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Chase; }
    }

    public class FollowMovementGenerator<T> : TargetedMovementGenerator<T> where T : Unit
    {
        public FollowMovementGenerator(Unit target) : base(target) { }

        public FollowMovementGenerator(Unit target, float offset, float angle) : base(target, offset, angle) { }

        public override void DoInitialize(T owner)
        {
            owner.AddUnitState(UnitState.Follow);
            UpdateSpeed(owner);
            SetTargetLocation(owner, true);
        }

        public override void DoReset(T owner)
        {
            DoInitialize(owner);
        }

        public override void DoFinalize(T owner)
        {
            owner.ClearUnitState(UnitState.Follow | UnitState.FollowMove);
            UpdateSpeed(owner);
        }

        public override void ClearUnitStateMove(T owner)
        {
            owner.ClearUnitState(UnitState.FollowMove);
        }

        public override void AddUnitStateMove(T owner)
        {
            owner.AddUnitState(UnitState.FollowMove);
        }

        public override void ReachTarget(T owner)
        {
            ClearUnitStateMove(owner);
        }

        public override bool EnableWalking()
        {
            if (typeof(T) == typeof(Player))
                return false;
            else
                return IsTargetValid() && GetTarget().IsWalking();
        }

        public override void MovementInform(T owner)
        {
            if (owner.IsTypeId(TypeId.Player))
                return;

            // Pass back the GUIDLow of the target. If it is pet's owner then PetAI will handle
            if (owner.ToCreature().GetAI() != null)
                owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Follow, (uint)GetTarget().GetGUID().GetCounter());
        }

        public override bool HasLostTarget(T u) { return false; }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Follow;
        }

        void UpdateSpeed(T owner)
        {
            if (owner.IsTypeId(TypeId.Player))
                return;

            if (!owner.IsPet() || !owner.IsInWorld || !IsTargetValid() && GetTarget().GetGUID() != owner.GetOwnerGUID())
                return;

            owner.UpdateSpeed(UnitMoveType.Run);
            owner.UpdateSpeed(UnitMoveType.Walk);
            owner.UpdateSpeed(UnitMoveType.Swim);
        }
    }
}
