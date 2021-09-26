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
    public class FollowMovementGenerator : AbstractFollower, IMovementGenerator
    {
        static uint CHECK_INTERVAL = 500;
        static float FOLLOW_RANGE_TOLERANCE = 1.0f;

        float _range;
        ChaseAngle _angle;

        uint _checkTimer = CHECK_INTERVAL;
        PathGenerator _path;
        Position _lastTargetPosition;

        public FollowMovementGenerator(Unit target, float range, ChaseAngle angle) : base(target)
        {
            _range = range;
            _angle = angle;
        }

        public void Initialize(Unit owner)
        {
            owner.AddUnitState(UnitState.Follow);
            UpdatePetSpeed(owner);
            _path = null;
        }

        public bool Update(Unit owner, uint diff)
        {
            // owner might be dead or gone
            if (!owner.IsAlive())
                return false;

            // our target might have gone away
            Unit target = GetTarget();
            if (target == null)
                return false;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                owner.StopMoving();
                return true;
            }

            if (owner.HasUnitState(UnitState.FollowMove))
            {
                if (_checkTimer > diff)
                    _checkTimer -= diff;
                else
                {
                    _checkTimer = CHECK_INTERVAL;
                    if (PositionOkay(owner, target, _range, _angle))
                    {
                        _path = null;
                        owner.StopMoving();
                        DoMovementInform(owner, target);
                        return true;
                    }
                }
            }

            if (owner.HasUnitState(UnitState.FollowMove) && owner.MoveSpline.Finalized())
            {
                _path = null;
                owner.ClearUnitState(UnitState.FollowMove);
                DoMovementInform(owner, target);
            }

            if (_lastTargetPosition.GetExactDistSq(target.GetPosition()) > 0.0f)
            {
                _lastTargetPosition = target.GetPosition();
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

                    MoveSplineInit init = new(owner);
                    init.MovebyPath(_path.GetPath());
                    init.SetWalk(target.IsWalking());
                    init.SetFacing(target.GetOrientation());
                    init.Launch();
                }
            }
            return true;
        }

        public void Finalize(Unit owner)
        {
            owner.ClearUnitState(UnitState.Follow | UnitState.FollowMove);
            UpdatePetSpeed(owner);
        }

        void UpdatePetSpeed(Unit owner)
        {
            Pet oPet = owner.ToPet();
            if (oPet != null)
            {
                if (!GetTarget() || GetTarget().GetGUID() == owner.GetOwnerGUID())
                {
                    oPet.UpdateSpeed(UnitMoveType.Run);
                    oPet.UpdateSpeed(UnitMoveType.Walk);
                    oPet.UpdateSpeed(UnitMoveType.Swim);
                }
            }
        }

        public void Reset(Unit owner) { }

        public MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Follow; }

        public void UnitSpeedChanged() { _lastTargetPosition.Relocate(0.0f, 0.0f, 0.0f); }

        static bool PositionOkay(Unit owner, Unit target, float range, ChaseAngle? angle = null)
        {
            if (owner.GetExactDistSq(target) > MathF.Sqrt(owner.GetCombatReach() + target.GetCombatReach() + range))
                return false;

            return !angle.HasValue || angle.Value.IsAngleOkay(target.GetRelativeAngle(owner));
        }

        static void DoMovementInform(Unit owner, Unit target)
        {
            if (!owner.IsCreature())
                return;

            UnitAI ai = owner.GetAI();
            if (ai != null)
                ((CreatureAI)ai).MovementInform(MovementGeneratorType.Follow, (uint)target.GetGUID().GetCounter());
        }
    }
}
