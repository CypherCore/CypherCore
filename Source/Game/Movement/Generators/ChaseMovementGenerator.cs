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
using Game.AI;
using Game.Entities;
using System;

namespace Game.Movement
{
    class ChaseMovementGenerator : MovementGenerator
    {
        static uint RANGE_CHECK_INTERVAL = 100; // time (ms) until we attempt to recalculate

        ChaseRange? _range;
        ChaseAngle? _angle;

        PathGenerator _path;
        Position _lastTargetPosition;
        uint _rangeCheckTimer = RANGE_CHECK_INTERVAL;
        bool _movingTowards = true;
        bool _mutualChase = true;

        AbstractFollower _abstractFollower;

        public ChaseMovementGenerator(Unit target, ChaseRange? range, ChaseAngle? angle)
        {
            _abstractFollower = new AbstractFollower(target);
            _range = range;
            _angle = angle;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Chase;
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            _path = null;
            _lastTargetPosition = null;
        }

        public override void Reset(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.Deactivated);
            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            // owner might be dead or gone (can we even get nullptr here?)
            if (!owner || !owner.IsAlive())
                return false;

            // our target might have gone away
            Unit target = _abstractFollower.GetTarget();
            if (target == null || !target.IsInWorld)
                return false;

            // the owner might be unable to move (rooted or casting), pause movement
            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                owner.StopMoving();
                _lastTargetPosition = null;
                Creature cOwner = owner.ToCreature();
                if (cOwner != null)
                    cOwner.SetCannotReachTarget(false);
                return true;
            }

            bool mutualChase = IsMutualChase(owner, target);
            float hitboxSum = owner.GetCombatReach() + target.GetCombatReach();
            float minRange = _range.HasValue ? _range.Value.MinRange + hitboxSum : SharedConst.ContactDistance;
            float minTarget = (_range.HasValue ? _range.Value.MinTolerance : 0.0f) + hitboxSum;
            float maxRange = _range.HasValue ? _range.Value.MaxRange + hitboxSum : owner.GetMeleeRange(target); // melee range already includes hitboxes
            float maxTarget = _range.HasValue ? _range.Value.MaxTolerance + hitboxSum : SharedConst.ContactDistance + hitboxSum;
            ChaseAngle? angle = mutualChase ? null : _angle;

            // if we're already moving, periodically check if we're already in the expected range...
            if (owner.HasUnitState(UnitState.ChaseMove))
            {
                if (_rangeCheckTimer > diff)
                    _rangeCheckTimer -= diff;
                else
                {
                    _rangeCheckTimer = RANGE_CHECK_INTERVAL;
                    if (PositionOkay(owner, target, _movingTowards ? null : minTarget, _movingTowards ? maxTarget : null, angle))
                    {
                        _path = null;
                        owner.StopMoving();
                        owner.SetInFront(target);
                        DoMovementInform(owner, target);
                        return true;
                    }
                }
            }

            // if we're done moving, we want to clean up
            if (owner.HasUnitState(UnitState.ChaseMove) && owner.MoveSpline.Finalized())
            {
                _path = null;
                Creature cOwner = owner.ToCreature();
                if (cOwner != null)
                    cOwner.SetCannotReachTarget(false);
                owner.ClearUnitState(UnitState.ChaseMove);
                owner.SetInFront(target);
                DoMovementInform(owner, target);
            }

            // if the target moved, we have to consider whether to adjust
            if (_lastTargetPosition == null || target.GetPosition() != _lastTargetPosition || mutualChase != _mutualChase)
            {
                _lastTargetPosition = new(target.GetPosition());
                _mutualChase = mutualChase;
                if (owner.HasUnitState(UnitState.ChaseMove) || !PositionOkay(owner, target, minRange, maxRange, angle))
                {
                    Creature cOwner = owner.ToCreature();
                    // can we get to the target?
                    if (cOwner != null && !target.IsInAccessiblePlaceFor(cOwner))
                    {
                        cOwner.SetCannotReachTarget(true);
                        cOwner.StopMoving();
                        _path = null;
                        return true;
                    }

                    // figure out which way we want to move
                    bool moveToward = !owner.IsInDist(target, maxRange);

                    // make a new path if we have to...
                    if (_path == null || moveToward != _movingTowards)
                        _path = new PathGenerator(owner);

                    float x, y, z;
                    bool shortenPath;
                    // if we want to move toward the target and there's no fixed angle...
                    if (moveToward && !angle.HasValue)
                    {
                        // ...we'll pathfind to the center, then shorten the path
                        target.GetPosition(out x, out y, out z);
                        shortenPath = true;
                    }
                    else
                    {
                        // otherwise, we fall back to nearpoint finding
                        target.GetNearPoint(owner, out x, out y, out z, (moveToward ? maxTarget : minTarget) - hitboxSum, angle.HasValue ? target.ToAbsoluteAngle(angle.Value.RelativeAngle) : target.GetAbsoluteAngle(owner));
                        shortenPath = false;
                    }

                    if (owner.IsHovering())
                        owner.UpdateAllowedPositionZ(x, y, ref z);

                    bool success = _path.CalculatePath(x, y, z, owner.CanFly());
                    if (!success || _path.GetPathType().HasAnyFlag(PathType.NoPath | PathType.Incomplete))
                    {
                        if (cOwner)
                            cOwner.SetCannotReachTarget(true);

                        owner.StopMoving();
                        return true;
                    }

                    if (shortenPath)
                        _path.ShortenPathUntilDist(target, maxTarget);

                    if (cOwner)
                        cOwner.SetCannotReachTarget(false);

                    owner.AddUnitState(UnitState.ChaseMove);

                    bool walk = false;
                    if (cOwner && !cOwner.IsPet())
                    {
                        switch (cOwner.GetMovementTemplate().GetChase())
                        {
                            case CreatureChaseMovementType.CanWalk:
                                walk = owner.IsWalking();
                                break;
                            case CreatureChaseMovementType.AlwaysWalk:
                                walk = true;
                                break;
                            default:
                                break;
                        }
                    }

                    MoveSplineInit init = new(owner);
                    init.MovebyPath(_path.GetPath());
                    init.SetWalk(walk);
                    init.SetFacing(target);

                    init.Launch();
                }
            }

            // and then, finally, we're done for the tick
            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.ChaseMove);
            Creature cOwner = owner.ToCreature();
            if (cOwner != null)
                cOwner.SetCannotReachTarget(false);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
            {
                owner.ClearUnitState(UnitState.ChaseMove);
                Creature cOwner = owner.ToCreature();
                if (cOwner != null)
                    cOwner.SetCannotReachTarget(false);
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Chase; }

        public override void UnitSpeedChanged() { _lastTargetPosition = null; }

        static bool IsMutualChase(Unit owner, Unit target)
        {
            if (target.GetMotionMaster().GetCurrentMovementGeneratorType() != MovementGeneratorType.Chase)
                return false;

            ChaseMovementGenerator movement = target.GetMotionMaster().GetCurrentMovementGenerator() as ChaseMovementGenerator;
            if (movement != null)
                return movement.GetTarget() == owner;

            return false;
        }

        static bool PositionOkay(Unit owner, Unit target, float? minDistance, float? maxDistance, ChaseAngle? angle)
        {
            float distSq = owner.GetExactDistSq(target);
            if (minDistance.HasValue && distSq < MathF.Sqrt(minDistance.Value))
                return false;
            if (maxDistance.HasValue && distSq > MathF.Sqrt(maxDistance.Value))
                return false;
            return !angle.HasValue || angle.Value.IsAngleOkay(target.GetRelativeAngle(owner));
        }

        static void DoMovementInform(Unit owner, Unit target)
        {
            if (!owner.IsCreature())
                return;

            CreatureAI ai = owner.ToCreature().GetAI();
            if (ai != null)
                ai.MovementInform(MovementGeneratorType.Chase, (uint)target.GetGUID().GetCounter());
        }

        public Unit GetTarget()
        {
            return _abstractFollower.GetTarget();
        }
    }
}
