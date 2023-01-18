// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        TimeTracker _rangeCheckTimer;
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

            _rangeCheckTimer = new(RANGE_CHECK_INTERVAL);
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized | MovementGeneratorFlags.InformEnabled);

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

            // the owner might be unable to move (rooted or casting), or we have lost the target, pause movement
            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting() || HasLostTarget(owner, target))
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

            // periodically check if we're already in the expected range...
            _rangeCheckTimer.Update(diff);
            if (_rangeCheckTimer.Passed())
            {
                _rangeCheckTimer.Reset(RANGE_CHECK_INTERVAL);
                if (HasFlag(MovementGeneratorFlags.InformEnabled) && PositionOkay(owner, target, _movingTowards ? null : minTarget, _movingTowards ? maxTarget : null, angle))
                {
                    RemoveFlag(MovementGeneratorFlags.InformEnabled);
                    _path = null;

                    Creature cOwner = owner.ToCreature();
                    if (cOwner != null)
                        cOwner.SetCannotReachTarget(false);

                    owner.StopMoving();
                    owner.SetInFront(target);
                    DoMovementInform(owner, target);
                    return true;
                }
            }

            // if we're done moving, we want to clean up
            if (owner.HasUnitState(UnitState.ChaseMove) && owner.MoveSpline.Finalized())
            {
                RemoveFlag(MovementGeneratorFlags.InformEnabled);
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
                    if (!success || _path.GetPathType().HasAnyFlag(PathType.NoPath))
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

                    owner.AddUnitState(UnitState.ChaseMove);
                    AddFlag(MovementGeneratorFlags.InformEnabled);

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
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.InformEnabled);
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

        static bool HasLostTarget(Unit owner, Unit target)
        {
            return owner.GetVictim() != target;
        }

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
            if (minDistance.HasValue && distSq < minDistance.Value * minDistance.Value)
                return false;
            if (maxDistance.HasValue && distSq > maxDistance.Value * maxDistance.Value)
                return false;
            if (angle.HasValue && !angle.Value.IsAngleOkay(target.GetRelativeAngle(owner)))
                return false;
            if (!owner.IsWithinLOSInMap(target))
                return false;
            return true;
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
