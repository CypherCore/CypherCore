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
using System.Collections.Generic;
using System.Linq;

namespace Game.Movement
{
    public class SplineChainMovementGenerator : IMovementGenerator
    {
        public SplineChainMovementGenerator(uint id, List<SplineChainLink> chain, bool walk = false)
        {
            _id = id;
            _chain = chain;
            _chainSize = (byte)chain.Count;
            _walk = walk;
        }
        public SplineChainMovementGenerator(SplineChainResumeInfo info)
        {
            _id = info.PointID;
            _chain = info.Chain;
            _chainSize = (byte)info.Chain.Count;
            _walk = info.IsWalkMode;
            finished = info.SplineIndex >= info.Chain.Count;
            _nextIndex = info.SplineIndex;
            _nextFirstWP = info.PointIndex;
            _msToNext = info.TimeToNext;
        }

        uint SendPathSpline(Unit me, Span<Vector3> wp)
        {
            int numWp = wp.Length;
            Cypher.Assert(numWp > 1, "Every path must have source & destination");
            MoveSplineInit init = new MoveSplineInit(me);
            if (numWp > 2)
                init.MovebyPath(wp.ToArray());
            else
                init.MoveTo(wp[1], false, true);
            init.SetWalk(_walk);
            return (uint)init.Launch();
        }

        void SendSplineFor(Unit me, int index, uint toNext)
        {
            Cypher.Assert(index < _chainSize);
            Log.outDebug(LogFilter.Movement, "{0}: Sending spline for {1}.", me.GetGUID().ToString(), index);

            SplineChainLink thisLink = _chain[index];
            uint actualDuration = SendPathSpline(me, new Span<Vector3>(thisLink.Points.ToArray()));
            if (actualDuration != thisLink.ExpectedDuration)
            {
                Log.outDebug(LogFilter.Movement, "{0}: Sent spline for {1}, duration is {2} ms. Expected was {3} ms (delta {4} ms). Adjusting.", me.GetGUID().ToString(), index, actualDuration, thisLink.ExpectedDuration, actualDuration - thisLink.ExpectedDuration);
                toNext = (uint)(actualDuration / thisLink.ExpectedDuration * toNext);
            }
            else
            {
                Log.outDebug(LogFilter.Movement, "{0}: Sent spline for {1}, duration is {2} ms.", me.GetGUID().ToString(), index, actualDuration);
            }
        }

        public override void Initialize(Unit me)
        {
            if (_chainSize != 0)
            {
                if (_nextFirstWP != 0) // this is a resumed movegen that has to start with a partial spline
                {
                    if (finished)
                        return;
                    SplineChainLink thisLink = _chain[_nextIndex];
                    if (_nextFirstWP >= thisLink.Points.Count)
                    {
                        Log.outError(LogFilter.Movement, "{0}: Attempted to resume spline chain from invalid resume state ({1}, {2}).", me.GetGUID().ToString(), _nextIndex, _nextFirstWP);
                        _nextFirstWP = (byte)(thisLink.Points.Count - 1);
                    }
                    Span<Vector3> span = thisLink.Points.ToArray();
                    SendPathSpline(me, span.Slice(_nextFirstWP - 1));
                    Log.outDebug(LogFilter.Movement, "{0}: Resumed spline chain generator from resume state.", me.GetGUID().ToString());
                    ++_nextIndex;
                    if (_msToNext == 0)
                        _msToNext = 1;
                    _nextFirstWP = 0;
                }
                else
                {
                    _msToNext = Math.Max(_chain[_nextIndex].TimeToNext, 1u);
                    SendSplineFor(me, _nextIndex, _msToNext);
                    ++_nextIndex;
                    if (_nextIndex >= _chainSize)
                        _msToNext = 0;
                }
            }
            else
            {
                Log.outError(LogFilter.Movement, "SplineChainMovementGenerator.Initialize - empty spline chain passed for {0}.", me.GetGUID().ToString());
            }
        }

        public override void Finalize(Unit me)
        {
            if (!finished)
                return;

            Creature cMe = me.ToCreature();
            if (cMe && cMe.IsAIEnabled)
                cMe.GetAI().MovementInform(MovementGeneratorType.SplineChain, _id);
        }

        public override bool Update(Unit me, uint diff)
        {
            if (finished)
                return false;

            // _msToNext being zero here means we're on the final spline
            if (_msToNext == 0)
            {
                finished = me.moveSpline.Finalized();
                return !finished;
            }

            if (_msToNext <= diff)
            {
                // Send next spline
                Log.outDebug(LogFilter.Movement, "{0}: Should send spline {1} ({2} ms late).", me.GetGUID().ToString(), _nextIndex, diff - _msToNext);
                _msToNext = Math.Max(_chain[_nextIndex].TimeToNext, 1u);
                SendSplineFor(me, _nextIndex, _msToNext);
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

        SplineChainResumeInfo GetResumeInfo(Unit me)
        {
            if (_nextIndex == 0)
                return new SplineChainResumeInfo(_id, _chain, _walk, 0, 0, _msToNext);
            if (me.moveSpline.Finalized())
            {
                if (_nextIndex < _chainSize)
                    return new SplineChainResumeInfo(_id, _chain, _walk, _nextIndex, 0, 1u);
                else
                    return new SplineChainResumeInfo();
            }
            return new SplineChainResumeInfo(_id, _chain, _walk, (byte)(_nextIndex - 1), (byte)(me.moveSpline._currentSplineIdx()), _msToNext);
        }

        public override void Reset(Unit owner) { }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.SplineChain; }

        uint _id;
        List<SplineChainLink> _chain = new List<SplineChainLink>();
        byte _chainSize;
        bool _walk;
        bool finished;
        byte _nextIndex;
        byte _nextFirstWP; // only used for resuming
        uint _msToNext;
    }
}
