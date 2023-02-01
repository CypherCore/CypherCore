// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Dynamic
{
    public class EventSystem
    {
        private readonly MultiMap<ulong, BasicEvent> _events = new();

        private ulong _time;

        public EventSystem()
        {
            _time = 0;
        }

        public void Update(uint p_time)
        {
            // update time
            _time += p_time;

            // main event loop
            KeyValuePair<ulong, BasicEvent> i;

            while ((i = _events.GetFirst()).Value != null && i.Key <= _time)
            {
                var Event = i.Value;
                _events.Remove(i);

                if (Event.IsRunning())
                {
                    Event.Execute(_time, p_time);

                    continue;
                }

                if (Event.IsAbortScheduled())
                {
                    Event.Abort(_time);
                    // Mark the event as aborted
                    Event.SetAborted();
                }

                if (Event.IsDeletable())
                    continue;

                // Reschedule non deletable events to be checked at
                // the next update tick
                AddEvent(Event, CalculateTime(TimeSpan.FromMilliseconds(1)), false);
            }
        }

        public void KillAllEvents(bool force)
        {
            foreach (var pair in _events.KeyValueList)
            {
                // Abort events which weren't aborted already
                if (!pair.Value.IsAborted())
                {
                    pair.Value.SetAborted();
                    pair.Value.Abort(_time);
                }

                // Skip non-deletable events when we are
                // not forcing the event cancellation.
                if (!force &&
                    !pair.Value.IsDeletable())
                    continue;

                if (!force)
                    _events.Remove(pair);
            }

            // fast clear event list (in force case)
            if (force)
                _events.Clear();
        }

        public void AddEvent(BasicEvent Event, TimeSpan e_time, bool set_addtime = true)
        {
            if (set_addtime)
                Event._addTime = _time;

            Event._execTime = (ulong)e_time.TotalMilliseconds;
            _events.Add((ulong)e_time.TotalMilliseconds, Event);
        }

        public void AddEvent(Action action, TimeSpan e_time, bool set_addtime = true)
        {
            AddEvent(new LambdaBasicEvent(action), e_time, set_addtime);
        }

        public void AddEventAtOffset(BasicEvent Event, TimeSpan offset)
        {
            AddEvent(Event, CalculateTime(offset));
        }

        public void AddEventAtOffset(BasicEvent Event, TimeSpan offset, TimeSpan offset2)
        {
            AddEvent(Event, CalculateTime(RandomHelper.RandTime(offset, offset2)));
        }

        public void AddEventAtOffset(Action action, TimeSpan offset)
        {
            AddEventAtOffset(new LambdaBasicEvent(action), offset);
        }

        public void ModifyEventTime(BasicEvent Event, TimeSpan newTime)
        {
            foreach (var pair in _events)
            {
                if (pair.Value != Event)
                    continue;

                Event._execTime = (ulong)newTime.TotalMilliseconds;
                _events.Remove(pair);
                _events.Add((ulong)newTime.TotalMilliseconds, Event);

                break;
            }
        }

        public TimeSpan CalculateTime(TimeSpan t_offset)
        {
            return TimeSpan.FromMilliseconds(_time) + t_offset;
        }

        public MultiMap<ulong, BasicEvent> GetEvents()
        {
            return _events;
        }
    }

    public class BasicEvent
    {
        public ulong _addTime;          // time when the event was added to queue, filled by event handler
        public ulong _execTime;         // planned time of next execution, filled by event handler
        private AbortState _abortState; // set by externals when the event is aborted, aborted events don't execute

        public BasicEvent()
        {
            _abortState = AbortState.Running;
        }

        public void ScheduleAbort()
        {
            Cypher.Assert(IsRunning(), "Tried to scheduled the abortion of an event twice!");
            _abortState = AbortState.Scheduled;
        }

        public void SetAborted()
        {
            Cypher.Assert(!IsAborted(), "Tried to abort an already aborted event!");
            _abortState = AbortState.Aborted;
        }

        // this method executes when the event is triggered
        // return false if event does not want to be deleted
        // e_time is execution time, p_time is update interval
        public virtual bool Execute(ulong e_time, uint p_time)
        {
            return true;
        }

        public virtual bool IsDeletable()
        {
            return true;
        } // this event can be safely deleted

        public virtual void Abort(ulong e_time)
        {
        } // this method executes when the event is aborted

        public bool IsRunning()
        {
            return _abortState == AbortState.Running;
        }

        public bool IsAbortScheduled()
        {
            return _abortState == AbortState.Scheduled;
        }

        public bool IsAborted()
        {
            return _abortState == AbortState.Aborted;
        }
    }

    internal class LambdaBasicEvent : BasicEvent
    {
        private readonly Action _callback;

        public LambdaBasicEvent(Action callback) : base()
        {
            _callback = callback;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            _callback();

            return true;
        }
    }

    internal enum AbortState
    {
        Running,
        Scheduled,
        Aborted
    }
}