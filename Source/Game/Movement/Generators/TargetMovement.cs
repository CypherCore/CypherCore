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
using Game.Entities;
using System;

namespace Game.Movement
{
    public interface TargetedMovementGeneratorBase
    {
        FollowerReference reftarget { get; set; }
        void stopFollowing();
    }

    public abstract class TargetedMovementGeneratorMedium<T> : MovementGeneratorMedium<T>, TargetedMovementGeneratorBase where T : Unit
    {
        public FollowerReference reftarget { get; set; }
        public Unit target
        {
            get { return reftarget.getTarget(); }
        }

        public void stopFollowing() { }

        protected TargetedMovementGeneratorMedium(Unit _target, float _offset = 0, float _angle = 0)
        {
            reftarget = new FollowerReference();
            reftarget.link(_target, this);
            recheckDistance = new TimeTrackerSmall();
            offset = _offset;
            angle = _angle;
            recalculateTravel = false;
            targetReached = false;
        }

        public override bool DoUpdate(T owner, uint time_diff)
        {
            if (!reftarget.isValid() || !target.IsInWorld)
                return false;

            if (owner == null || !owner.IsAlive())
                return false;

            if (owner.HasUnitState(UnitState.NotMove))
            {
                _clearUnitStateMove(owner);
                return true;
            }

            // prevent movement while casting spells with cast time or channel time
            if (owner.IsMovementPreventedByCasting())
            {
                if (!owner.isStopped())
                    owner.StopMoving();
                return true;
            }

            // prevent crash after creature killed pet
            if (_lostTarget(owner))
            {
                _clearUnitStateMove(owner);
                return true;
            }

            bool targetMoved = false;
            recheckDistance.Update((int)time_diff);
            if (recheckDistance.Passed())
            {
                recheckDistance.Reset(100);
                //More distance let have better performance, less distance let have more sensitive reaction at target move.
                float allowed_dist = 0.0f;// owner.GetCombatReach() + WorldConfig.GetFloatValue(WorldCfg.RateTargetPosRecalculationRange);

                if (owner.IsPet() && (owner.GetCharmerOrOwnerGUID() == target.GetGUID()))
                    allowed_dist = 1.0f; // pet following owner
                else
                    allowed_dist = owner.GetCombatReach() + WorldConfig.GetFloatValue(WorldCfg.RateTargetPosRecalculationRange);

                Vector3 dest = owner.moveSpline.FinalDestination();
                if (owner.moveSpline.onTransport)
                {
                    float o = 0;
                    ITransport transport = owner.GetDirectTransport();
                    if (transport != null)
                        transport.CalculatePassengerPosition(ref dest.X, ref dest.Y, ref dest.Z, ref o);
                }

                // First check distance
                if (owner.IsTypeId(TypeId.Unit) && (owner.ToCreature().CanFly() || owner.ToCreature().CanSwim()))
                    targetMoved = !target.IsWithinDist3d(dest.X, dest.Y, dest.Z, allowed_dist);
                else
                    targetMoved = !target.IsWithinDist2d(dest.X, dest.Y, allowed_dist);


                // then, if the target is in range, check also Line of Sight.
                if (!targetMoved)
                    targetMoved = !target.IsWithinLOSInMap(owner);
            }

            if (recalculateTravel || targetMoved)
                _setTargetLocation(owner, targetMoved);

            if (owner.moveSpline.Finalized())
            {
                MovementInform(owner);
                if (angle == 0.0f && !owner.HasInArc(0.01f, target))
                    owner.SetInFront(target);

                if (!targetReached)
                {
                    targetReached = true;
                    _reachTarget(owner);
                }
            }

            return true;
        }

        public override void unitSpeedChanged()
        {
            recalculateTravel = true;
        }

        public void _setTargetLocation(T owner, bool updateDestination)
        {
            if (!reftarget.isValid() || !target.IsInWorld)
                return;

            if (owner.HasUnitState(UnitState.NotMove))
                return;

            if (owner.IsMovementPreventedByCasting())
                return;

            if (owner.IsTypeId(TypeId.Unit) && !target.isInAccessiblePlaceFor(owner.ToCreature()))
            {
                owner.ToCreature().SetCannotReachTarget(true);
                return;
            }

            if (owner.IsTypeId(TypeId.Unit) && owner.ToCreature().IsFocusing(null, true))
                return;

            float x, y, z;
            if (updateDestination || i_path == null)
            {
                if (offset == 0)
                {
                    if (target.IsWithinDistInMap(owner, SharedConst.ContactDistance))
                        return;

                    // to nearest contact position
                    target.GetContactPoint(owner, out x, out y, out z);
                }
                else
                {
                    float dist = 0;
                    float size = 0;

                    // Pets need special handling.
                    // We need to subtract GetObjectSize() because it gets added back further down the chain
                    //  and that makes pets too far away. Subtracting it allows pets to properly
                    //  be (GetCombatReach() + i_offset) away.
                    // Only applies when i_target is pet's owner otherwise pets and mobs end up
                    //   doing a "dance" while fighting
                    if (owner.IsPet() && target.IsTypeId(TypeId.Player))
                    {
                        dist = 1.0f;// target.GetCombatReach();
                        size = 1.0f;// target.GetCombatReach() - target.GetObjectSize();
                    }
                    else
                    {
                        dist = offset + 1.0f;
                        size = owner.GetObjectSize();
                    }

                    if (target.IsWithinDistInMap(owner, dist))
                        return;

                    // to at i_offset distance from target and i_angle from target facing
                    target.GetClosePoint(out x, out y, out z, size, offset, angle);
                }
            }
            else
            {
                // the destination has not changed, we just need to refresh the path (usually speed change)
                var end = i_path.GetEndPosition();
                x = end.X;
                y = end.Y;
                z = end.Z;
            }

            if (i_path == null)
                i_path = new PathGenerator(owner);

            // allow pets to use shortcut if no path found when following their master
            bool forceDest = (owner.IsTypeId(TypeId.Unit) && owner.IsPet()
                && owner.HasUnitState(UnitState.Follow));

            bool result = i_path.CalculatePath(x, y, z, forceDest);
            if (!result && Convert.ToBoolean(i_path.GetPathType() & PathType.NoPath))
            {
                // Can't reach target
                recalculateTravel = true;
                if (owner.IsTypeId(TypeId.Unit))
                    owner.ToCreature().SetCannotReachTarget(true);
                return;
            }

            _addUnitStateMove(owner);
            targetReached = false;
            recalculateTravel = false;
            owner.AddUnitState(UnitState.Chase);
            if (owner.IsTypeId(TypeId.Unit))
                owner.ToCreature().SetCannotReachTarget(false);

            MoveSplineInit init = new MoveSplineInit(owner);
            init.MovebyPath(i_path.GetPath());
            init.SetWalk(EnableWalking());
            // Using the same condition for facing target as the one that is used for SetInFront on movement end
            // - applies to ChaseMovementGenerator mostly
            if (angle == 0.0f)
                init.SetFacing(target);

            init.Launch();
        }

        public void UpdateFinalDistance(float fDistance)
        {
            if (typeof(T) == typeof(Player))
                return;
            offset = fDistance;
            recalculateTravel = true;
        }

        bool IsReachable() { return (i_path != null) ? Convert.ToBoolean(i_path.GetPathType() & PathType.Normal) : true; }

        public abstract void MovementInform(T unit);
        public abstract bool _lostTarget(T u);
        public abstract void _clearUnitStateMove(T u);
        public abstract void _addUnitStateMove(T u);
        public abstract void _reachTarget(T owner);
        public abstract bool EnableWalking();
        public abstract void _updateSpeed(T u);

        #region Fields
        PathGenerator i_path;
        TimeTrackerSmall recheckDistance;
        float offset;
        float angle;
        public bool recalculateTravel;
        bool targetReached;
        #endregion
    }

    public class ChaseMovementGenerator<T> : TargetedMovementGeneratorMedium<T> where T : Unit
    {
        public ChaseMovementGenerator(Unit target)
            : base(target)
        {
        }
        public ChaseMovementGenerator(Unit target, float offset, float angle)
            : base(target, offset, angle)
        {
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Chase; }

        public override void DoInitialize(T owner)
        {
            if (owner.IsTypeId(TypeId.Unit))
                owner.SetWalk(false);

            owner.AddUnitState(UnitState.Chase | UnitState.ChaseMove);
            _setTargetLocation(owner, true);
        }

        public override void DoFinalize(T owner)
        {
            owner.ClearUnitState(UnitState.Chase | UnitState.ChaseMove);
        }

        public override void DoReset(T owner)
        {
            DoInitialize(owner);
        }

        public override bool _lostTarget(T u)
        {
            return u.GetVictim() != target;
        }
        public override void _clearUnitStateMove(T u)
        {
            u.ClearUnitState(UnitState.ChaseMove);
        }
        public override void _addUnitStateMove(T u)
        {
            u.AddUnitState(UnitState.ChaseMove);
        }

        public override bool EnableWalking() { return false; }
        public override void _updateSpeed(T u) { }
        public override void _reachTarget(T owner)
        {
            _clearUnitStateMove(owner);
            if (owner.IsWithinMeleeRange(target))
                owner.Attack(target, true);
            if (owner.IsTypeId(TypeId.Unit))
                owner.ToCreature().SetCannotReachTarget(false);
        }
        public override void MovementInform(T unit)
        {
            if (unit.IsTypeId(TypeId.Unit))
            {
                // Pass back the GUIDLow of the target. If it is pet's owner then PetAI will handle
                if (unit.ToCreature().GetAI() != null)
                    unit.ToCreature().GetAI().MovementInform(MovementGeneratorType.Chase, (uint)target.GetGUID().GetCounter());
            }
        }
    }

    public class FollowMovementGenerator<T> : TargetedMovementGeneratorMedium<T> where T : Unit
    {
        public FollowMovementGenerator(Unit target) : base(target) { }
        public FollowMovementGenerator(Unit target, float offset, float angle) : base(target, offset, angle) { }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Follow;
        }
        public override void _clearUnitStateMove(T u)
        {
            u.ClearUnitState(UnitState.FollowMove);
        }
        public override void _addUnitStateMove(T u)
        {
            u.AddUnitState(UnitState.FollowMove);
        }
        public override void DoReset(T owner)
        {
            DoInitialize(owner);
        }
        public override bool _lostTarget(T u) { return false; }
        public override void _reachTarget(T u) { }

        public override void DoInitialize(T owner)
        {
            owner.AddUnitState(UnitState.Follow | UnitState.FollowMove);
            _updateSpeed(owner);
            _setTargetLocation(owner, true);
        }
        public override void DoFinalize(T owner)
        {
            owner.ClearUnitState(UnitState.Follow | UnitState.FollowMove);
            _updateSpeed(owner);
        }
        public override void MovementInform(T unit)
        {
            if (unit.IsTypeId(TypeId.Player))
                return;

            // Pass back the GUIDLow of the target. If it is pet's owner then PetAI will handle
            if (unit.ToCreature().GetAI() != null)
                unit.ToCreature().GetAI().MovementInform(MovementGeneratorType.Follow, (uint)target.GetGUID().GetCounter());
        }
        public override bool EnableWalking()
        {
            if (typeof(T) == typeof(Player))
                return false;
            else
                return reftarget.isValid() && target.IsWalking();
        }
        public override void _updateSpeed(T owner)
        {
            if (owner.IsTypeId(TypeId.Player))
                return;

            if (!owner.IsPet() || !owner.IsInWorld || !reftarget.isValid() && target.GetGUID() != owner.GetOwnerGUID())
                return;

            owner.UpdateSpeed(UnitMoveType.Run);
            owner.UpdateSpeed(UnitMoveType.Walk);
            owner.UpdateSpeed(UnitMoveType.Swim);
        }
    }
}
