// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Movement
{
    public class SplineChainMovementGenerator : MovementGenerator
    {
        public SplineChainMovementGenerator(uint id, List<SplineChainLink> chain, bool walk = false)
        {
            _id = id;
            _chain = chain;
            _chainSize = (byte)chain.Count;
            _walk = walk;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
        }

        public SplineChainMovementGenerator(SplineChainResumeInfo info)
        {
            _id = info.PointID;
            _chain = info.Chain;
            _chainSize = (byte)info.Chain.Count;
            _walk = info.IsWalkMode;
            _nextIndex = info.SplineIndex;
            _nextFirstWP = info.PointIndex;
            _msToNext = info.TimeToNext;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            if (info.SplineIndex >= info.Chain.Count)
                AddFlag(MovementGeneratorFlags.Finalized);

            BaseUnitState = UnitState.Roaming;
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (_chainSize == 0)
            {
                Log.outError(LogFilter.Movement, $"SplineChainMovementGenerator::Initialize: couldn't initialize generator, referenced spline is empty! ({owner.GetGUID()})");
                return;
            }

            if (_nextIndex >= _chainSize)
            {
                Log.outWarn(LogFilter.Movement, $"SplineChainMovementGenerator::Initialize: couldn't initialize generator, _nextIndex is >= _chainSize ({owner.GetGUID()})");
                _msToNext = 0;
                return;
            }

            if (_nextFirstWP != 0) // this is a resumed movegen that has to start with a partial spline
            {

                if (HasFlag(MovementGeneratorFlags.Finalized))
                    return;

                SplineChainLink thisLink = _chain[_nextIndex];
                if (_nextFirstWP >= thisLink.Points.Count)
                {
                    Log.outError(LogFilter.Movement, $"SplineChainMovementGenerator::Initialize: attempted to resume spline chain from invalid resume state, _nextFirstWP >= path size (_nextIndex: {_nextIndex}, _nextFirstWP: {_nextFirstWP}). ({owner.GetGUID()})");
                    _nextFirstWP = (byte)(thisLink.Points.Count - 1);
                }

                owner.AddUnitState(UnitState.RoamingMove);
                Span<Vector3> partial = thisLink.Points.ToArray();
                SendPathSpline(owner, thisLink.Velocity, partial[(_nextFirstWP - 1)..]);

                Log.outDebug(LogFilter.Movement, $"SplineChainMovementGenerator::Initialize: resumed spline chain generator from resume state. ({owner.GetGUID()})");

                ++_nextIndex;
                if (_nextIndex >= _chainSize)
                    _msToNext = 0;
                else if (_msToNext == 0)
                    _msToNext = 1;
                _nextFirstWP = 0;
            }
            else
            {
                _msToNext = Math.Max(_chain[_nextIndex].TimeToNext, 1u);
                SendSplineFor(owner, _nextIndex, ref _msToNext);

                ++_nextIndex;
                if (_nextIndex >= _chainSize)
                    _msToNext = 0;
            }
        }

        public override void Reset(Unit owner) 
        {
            RemoveFlag(MovementGeneratorFlags.Deactivated);

            owner.StopMoving();
            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (owner == null || HasFlag(MovementGeneratorFlags.Finalized))
                return false;

            // _msToNext being zero here means we're on the final spline
            if (_msToNext == 0)
            {
                if (owner.MoveSpline.Finalized())
                {
                    AddFlag(MovementGeneratorFlags.InformEnabled);
                    return false;
                }
                return true;
            }

            if (_msToNext <= diff)
            {
                // Send next spline
                Log.outDebug(LogFilter.Movement, $"SplineChainMovementGenerator::Update: sending spline on index {_nextIndex} ({diff - _msToNext} ms late). ({owner.GetGUID()})");
                _msToNext = Math.Max(_chain[_nextIndex].TimeToNext, 1u);
                SendSplineFor(owner, _nextIndex, ref _msToNext);
                ++_nextIndex;
                if (_nextIndex >= _chainSize)
                {
                    // We have reached the final spline, once it finalizes we should also finalize the movegen (start checking on next update)
                    _msToNext = 0;
                    return true;
                }
            }
            else
                _msToNext -= diff;

            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.RoamingMove);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);

            if (active)
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
            {
                CreatureAI ai = owner.ToCreature().GetAI();
                if (ai != null)
                    ai.MovementInform(MovementGeneratorType.SplineChain, _id);
            }
        }

        uint SendPathSpline(Unit owner, float velocity, Span<Vector3> path)
        {
            int nodeCount = path.Length;
            Cypher.Assert(nodeCount > 1, $"SplineChainMovementGenerator::SendPathSpline: Every path must have source & destination (size > 1)! ({owner.GetGUID()})");

            MoveSplineInit init = new(owner);
            if (nodeCount > 2)
                init.MovebyPath(path.ToArray());
            else
                init.MoveTo(path[1], false, true);

            if (velocity > 0.0f)
                init.SetVelocity(velocity);

            init.SetWalk(_walk);
            return (uint)init.Launch();
        }

        void SendSplineFor(Unit owner, int index, ref uint duration)
        {
            Cypher.Assert(index < _chainSize, $"SplineChainMovementGenerator::SendSplineFor: referenced index ({index}) higher than path size ({_chainSize})!");
            Log.outDebug(LogFilter.Movement, $"SplineChainMovementGenerator::SendSplineFor: sending spline on index: {index}. ({owner.GetGUID()})");

            SplineChainLink thisLink = _chain[index];
            uint actualDuration = SendPathSpline(owner, thisLink.Velocity, new Span<Vector3>(thisLink.Points.ToArray()));
            if (actualDuration != thisLink.ExpectedDuration)
            {
                Log.outDebug(LogFilter.Movement, $"SplineChainMovementGenerator::SendSplineFor: sent spline on index: {index}, duration: {actualDuration} ms. Expected duration: {thisLink.ExpectedDuration} ms (delta {actualDuration - thisLink.ExpectedDuration} ms). Adjusting. ({owner.GetGUID()})");
                duration = (uint)((double)actualDuration / (double)thisLink.ExpectedDuration * duration);
            }
            else
            {
                Log.outDebug(LogFilter.Movement, $"SplineChainMovementGenerator::SendSplineFor: sent spline on index {index}, duration: {actualDuration} ms. ({owner.GetGUID()})");
            }
        }

        SplineChainResumeInfo GetResumeInfo(Unit owner)
        {
            if (_nextIndex == 0)
                return new SplineChainResumeInfo(_id, _chain, _walk, 0, 0, _msToNext);

            if (owner.MoveSpline.Finalized())
            {
                if (_nextIndex < _chainSize)
                    return new SplineChainResumeInfo(_id, _chain, _walk, _nextIndex, 0, 1u);
                else
                    return new SplineChainResumeInfo();
            }

            return new SplineChainResumeInfo(_id, _chain, _walk, (byte)(_nextIndex - 1), (byte)owner.MoveSpline.CurrentSplineIdx(), _msToNext);
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.SplineChain; }

        public uint GetId() { return _id; }

    uint _id;
        List<SplineChainLink> _chain = new();
        byte _chainSize;
        bool _walk;
        byte _nextIndex;
        byte _nextFirstWP; // only used for resuming
        uint _msToNext;
    }
}
