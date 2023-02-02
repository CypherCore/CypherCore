// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using System;

namespace Game.Movement
{
    public class FollowMovementGenerator : MovementGenerator
    {
        static uint CHECK_INTERVAL = 100;
        static float FOLLOW_RANGE_TOLERANCE = 1.0f;

        float _range;
        ChaseAngle _angle;

        TimeTracker _checkTimer;
        PathGenerator _path;
        Position _lastTargetPosition;

        AbstractFollower _abstractFollower;

        public FollowMovementGenerator(Unit target, float range, ChaseAngle angle)
        {
            _abstractFollower = new AbstractFollower(target);
            _range = range;
            _angle = angle;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Follow;

            _checkTimer = new(CHECK_INTERVAL);
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized | MovementGeneratorFlags.InformEnabled);

            owner.StopMoving();
            UpdatePetSpeed(owner);
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
            // owner might be dead or gone
            if (owner == null || !owner.IsAlive())
                return false;

            // our target might have gone away
            Unit target = _abstractFollower.GetTarget();
            if (target == null || !target.IsInWorld)
                return false;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                _path = null;
                owner.StopMoving();
                _lastTargetPosition = null;
                return true;
            }

            _checkTimer.Update(diff);
            if (_checkTimer.Passed())
            {
                _checkTimer.Reset(CHECK_INTERVAL);
                if (HasFlag(MovementGeneratorFlags.InformEnabled) && PositionOkay(owner, target, _range, _angle))
                {
                    RemoveFlag(MovementGeneratorFlags.InformEnabled);
                    _path = null;
                    owner.StopMoving();
                    _lastTargetPosition = new();
                    DoMovementInform(owner, target);
                    return true;
                }
            }

            if (owner.HasUnitState(UnitState.FollowMove) && owner.MoveSpline.Finalized())
            {
                RemoveFlag(MovementGeneratorFlags.InformEnabled);
                _path = null;
                owner.ClearUnitState(UnitState.FollowMove);
                DoMovementInform(owner, target);
            }

            if (_lastTargetPosition == null || _lastTargetPosition.GetExactDistSq(target.GetPosition()) > 0.0f)
            {
                _lastTargetPosition = new(target.GetPosition());
                if (owner.HasUnitState(UnitState.FollowMove) || !PositionOkay(owner, target, _range + FOLLOW_RANGE_TOLERANCE))
                {
                    if (_path == null)
                        _path = new PathGenerator(owner);

                    float x, y, z;

                    // select angle
                    float tAngle;
                    float curAngle = target.GetRelativeAngle(owner);
                    if (_angle.IsAngleOkay(curAngle))
                        tAngle = curAngle;
                    else
                    {
                        float diffUpper = Position.NormalizeOrientation(curAngle - _angle.UpperBound());
                        float diffLower = Position.NormalizeOrientation(_angle.LowerBound() - curAngle);
                        if (diffUpper < diffLower)
                            tAngle = _angle.UpperBound();
                        else
                            tAngle = _angle.LowerBound();
                    }

                    target.GetNearPoint(owner, out x, out y, out z, _range, target.ToAbsoluteAngle(tAngle));

                    if (owner.IsHovering())
                        owner.UpdateAllowedPositionZ(x, y, ref z);

                    // pets are allowed to "cheat" on pathfinding when following their master
                    bool allowShortcut = false;
                    Pet oPet = owner.ToPet();
                    if (oPet != null)
                        if (target.GetGUID() == oPet.GetOwnerGUID())
                            allowShortcut = true;

                    bool success = _path.CalculatePath(x, y, z, allowShortcut);
                    if (!success || _path.GetPathType().HasFlag(PathType.NoPath))
                    {
                        owner.StopMoving();
                        return true;
                    }

                    owner.AddUnitState(UnitState.FollowMove);
                    AddFlag(MovementGeneratorFlags.InformEnabled);

                    MoveSplineInit init = new(owner);
                    init.MovebyPath(_path.GetPath());
                    init.SetWalk(target.IsWalking());
                    init.SetFacing(target.GetOrientation());
                    init.Launch();
                }
            }
            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.InformEnabled);
            owner.ClearUnitState(UnitState.FollowMove);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
            {
                owner.ClearUnitState(UnitState.FollowMove);
                UpdatePetSpeed(owner);
            }
        }

        void UpdatePetSpeed(Unit owner)
        {
            Pet oPet = owner.ToPet();
            if (oPet != null)
            {
                if (!_abstractFollower.GetTarget() || _abstractFollower.GetTarget().GetGUID() == owner.GetOwnerGUID())
                {
                    oPet.UpdateSpeed(UnitMoveType.Run);
                    oPet.UpdateSpeed(UnitMoveType.Walk);
                    oPet.UpdateSpeed(UnitMoveType.Swim);
                }
            }
        }

        public Unit GetTarget()
        {
            return _abstractFollower.GetTarget();
        }



        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Follow; }

        public override void UnitSpeedChanged() { _lastTargetPosition = null; }

        static bool PositionOkay(Unit owner, Unit target, float range, ChaseAngle? angle = null)
        {
            if (owner.GetExactDistSq(target) > (owner.GetCombatReach() + target.GetCombatReach() + range) * (owner.GetCombatReach() + target.GetCombatReach() + range))
                return false;

            return !angle.HasValue || angle.Value.IsAngleOkay(target.GetRelativeAngle(owner));
        }

        static void DoMovementInform(Unit owner, Unit target)
        {
            if (!owner.IsCreature())
                return;

            CreatureAI ai = owner.ToCreature().GetAI();
            if (ai != null)
                ai.MovementInform(MovementGeneratorType.Follow, (uint)target.GetGUID().GetCounter());
        }
    }
}
