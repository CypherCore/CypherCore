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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Dynamic
{
    public class EventMap
    {
        /// <summary>
        /// Removes all scheduled events and resets time and phase.
        /// </summary>
        public void Reset()
        {
            _eventMap.Clear();
            _time = 0;
            _phase = 0;
        }

        /// <summary>
        /// Updates the timer of the event map.
        /// </summary>
        /// <param name="time">Value in ms to be added to time.</param>
        public void Update(uint time)
        {
            _time += time;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Current timer in ms value.</returns>
        uint GetTimer()
        {
            return _time;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Active phases as mask.</returns>
        byte GetPhaseMask()
        {
            return _phase;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True, if there are no events scheduled.</returns>
        public bool Empty()
        {
            return _eventMap.Empty();
        }

        /// <summary>
        /// Sets the phase of the map (absolute).
        /// </summary>
        /// <param name="phase">Phase which should be set. Values: 1 - 8. 0 resets phase.</param>
        public void SetPhase(byte phase)
        {
            if (phase == 0)
                _phase = 0;
            else if (phase <= 8)
                _phase = (byte)(1 << (phase - 1));
        }

        /// <summary>
        /// Activates the given phase (bitwise).
        /// </summary>
        /// <param name="phase">Phase which should be activated. Values: 1 - 8</param>
        void AddPhase(byte phase)
        {
            if (phase != 0 && phase <= 8)
                _phase |= (byte)(1 << (phase - 1));
        }

        /// <summary>
        /// Deactivates the given phase (bitwise).
        /// </summary>
        /// <param name="phase">Phase which should be deactivated. Values: 1 - 8.</param>
        void RemovePhase(byte phase)
        {
            if (phase != 0 && phase <= 8)
                _phase &= (byte)~(1 << (phase - 1));
        }

        /// <summary>
        /// Creates new event entry in map.
        /// </summary>
        /// <param name="eventId">The id of the new event.</param>
        /// <param name="time">The time in milliseconds as TimeSpan until the event occurs.</param>
        /// <param name="group">The group which the event is associated to. Has to be between 1 and 8. 0 means it has no group.</param>
        /// <param name="phase">The phase in which the event can occur. Has to be between 1 and 8. 0 means it can occur in all phases.</param>
        public void ScheduleEvent(uint eventId, TimeSpan time, uint group = 0, byte phase = 0)
        {
            ScheduleEvent(eventId, (uint)time.TotalMilliseconds, group, phase);
        }

        /// <summary>
        /// Creates new event entry in map.
        /// </summary>
        /// <param name="eventId">The id of the new event.</param>
        /// <param name="minTime">The minimum time until the event occurs as TimeSpan type.</param>
        /// <param name="maxTime">The maximum time until the event occurs as TimeSpan type.</param>
        /// <param name="group">The group which the event is associated to. Has to be between 1 and 8. 0 means it has no group.</param>
        /// <param name="phase">The phase in which the event can occur. Has to be between 1 and 8. 0 means it can occur in all phases.</param>
        public void ScheduleEvent(uint eventId, TimeSpan minTime, TimeSpan maxTime, uint group = 0, byte phase = 0)
        {
            ScheduleEvent(eventId, RandomHelper.URand(minTime.TotalMilliseconds, maxTime.TotalMilliseconds), group, phase);
        }
        

        /// <summary>
        /// Creates new event entry in map.
        /// </summary>
        /// <param name="eventId">The id of the new event.</param>
        /// <param name="time">The time in milliseconds until the event occurs.</param>
        /// <param name="group">The group which the event is associated to. Has to be between 1 and 8. 0 means it has no group.</param>
        /// <param name="phase">The phase in which the event can occur. Has to be between 1 and 8. 0 means it can occur in all phases.</param>
        public void ScheduleEvent(uint eventId, uint time, uint group = 0, byte phase = 0)
        {
            if (group != 0 && group <= 8)
                eventId |= (uint)(1 << ((int)group + 15));

            if (phase != 0 && phase <= 8)
                eventId |= (uint)(1 << (phase + 23));

            _eventMap.Add(_time + time, eventId);
        }

        /// <summary>
        /// Cancels the given event and reschedules it.
        /// </summary>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="time">The time in milliseconds as TimeSpan until the event occurs.</param>
        /// <param name="group">The group which the event is associated to. Has to be between 1 and 8. 0 means it has no group.</param>
        /// <param name="phase">The phase in which the event can occur. Has to be between 1 and 8. 0 means it can occur in all phases.</param>
        public void RescheduleEvent(uint eventId, TimeSpan time, uint group = 0, byte phase = 0)
        {
            RescheduleEvent(eventId, (uint)time.TotalMilliseconds, group, phase);
        }

        /// <summary>
        /// Cancels the given event and reschedules it.
        /// </summary>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="minTime">The minimum time until the event occurs as TimeSpan type.</param>
        /// <param name="maxTime">The maximum time until the event occurs as TimeSpan type.</param>
        /// <param name="group">The group which the event is associated to. Has to be between 1 and 8. 0 means it has no group.</param>
        /// <param name="phase">The phase in which the event can occur. Has to be between 1 and 8. 0 means it can occur in all phases.</param>
        void RescheduleEvent(uint eventId, TimeSpan minTime, TimeSpan maxTime, uint group = 0, byte phase = 0)
        {
            RescheduleEvent(eventId, RandomHelper.URand(minTime.TotalMilliseconds, maxTime.TotalMilliseconds), group, phase);
        }
        
        /// <summary>
        /// Cancels the given event and reschedules it.
        /// </summary>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="time">The time in milliseconds until the event occurs.</param>
        /// <param name="group">The group which the event is associated to. Has to be between 1 and 8. 0 means it has no group.</param>
        /// <param name="phase">The phase in which the event can occur. Has to be between 1 and 8. 0 means it can occur in all phases.</param>
        public void RescheduleEvent(uint eventId, uint time, uint group = 0, byte phase = 0)
        {
            CancelEvent(eventId);
            ScheduleEvent(eventId, time, group, phase);
        }

        /// <summary>
        /// Repeats the mostly recently executed event.
        /// </summary>
        /// <param name="time">Time until in ms as TimeSpan the event occurs</param>
        public void Repeat(TimeSpan time)
        {
            Repeat((uint)time.TotalMilliseconds);
        }

        /// <summary>
        /// Repeats the mostly recently executed event.
        /// </summary>
        /// <param name="time">Time until the event occurs</param>
        public void Repeat(uint time)
        {
            _eventMap.Add(_time + time, _lastEvent);
        }

        /// <summary>
        /// Repeats the mostly recently executed event. Equivalent to Repeat(urand(minTime, maxTime)
        /// </summary>
        /// <param name="minTime">Min Time as TimeSpan until the event occurs.</param>
        /// <param name="maxTime">Max Time as TimeSpan until the event occurs.</param>
        public void Repeat(TimeSpan minTime, TimeSpan maxTime)
        {
            Repeat((uint)minTime.TotalMilliseconds, (uint)maxTime.TotalMilliseconds);
        }

        /// <summary>
        /// Repeats the mostly recently executed event. Equivalent to Repeat(urand(minTime, maxTime)
        /// </summary>
        /// <param name="minTime">Min Time until the event occurs.</param>
        /// <param name="maxTime">Max Time until the event occurs.</param>
        public void Repeat(uint minTime, uint maxTime)
        {
            Repeat(RandomHelper.URand(minTime, maxTime));
        }

        /// <summary>
        /// Returns the next event to execute and removes it from map.
        /// </summary>
        /// <returns>Id of the event to execute.</returns>
        ///
        public uint ExecuteEvent()
        {
            while (!Empty())
            {
                var pair = _eventMap.FirstOrDefault();

                if (pair.Key > _time)
                    return 0;
                else if (_phase != 0 && Convert.ToBoolean(pair.Value & 0xFF000000) && !Convert.ToBoolean((pair.Value >> 24) & _phase))
                    _eventMap.Remove(pair);
                else
                {
                    uint eventId = (pair.Value & 0x0000FFFF);
                    _lastEvent = pair.Value; // include phase/group
                    _eventMap.Remove(pair);
                    return eventId;
                }
            }

            return 0;
        }

        public void ExecuteEvents(Action<uint> action)
        {
            uint id;
            while ((id = ExecuteEvent()) != 0)
                action(id);
        }

        /// <summary>
        /// Delays all events in the map. If delay is greater than or equal internal timer, delay will be 0.
        /// </summary>
        /// <param name="delay">Amount of delay in ms as TimeSpan.</param>
        public void DelayEvents(TimeSpan delay)
        {
            DelayEvents((uint)delay.TotalMilliseconds);
        }

        /// <summary>
        /// Delays all events in the map. If delay is greater than or equal internal timer, delay will be 0.
        /// </summary>
        /// <param name="delay">Amount of delay.</param>
        public void DelayEvents(uint delay)
        {
            _time = delay < _time ? _time - delay : 0;
        }

        /// <summary>
        /// Delay all events of the same group.
        /// </summary>
        /// <param name="delay">Amount of delay.</param>
        /// <param name="group">Group of the events.</param>
        public void DelayEvents(uint delay, uint group)
        {
            if (group == 0 || group > 8 || Empty())
                return;
            MultiMap<uint, uint> delayed = new MultiMap<uint, uint>();

            foreach (var pair in _eventMap.KeyValueList)
            {
                if (Convert.ToBoolean(pair.Value & (1 << (int)(group + 15))))
                {
                    delayed.Add(pair.Key + delay, pair.Value);
                    _eventMap.Remove(pair.Key, pair.Value);
                }
            }

            foreach (var del in delayed)
                _eventMap.Add(del);
        }

        /// <summary>
        /// Cancels all events of the specified id.
        /// </summary>
        /// <param name="eventId">Event id to cancel.</param>
        public void CancelEvent(uint eventId)
        {
            if (Empty())
                return;

            foreach (var pair in _eventMap.KeyValueList)
            {
                if (eventId == (pair.Value & 0x0000FFFF))
                    _eventMap.Remove(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Cancel events belonging to specified group.
        /// </summary>
        /// <param name="group">Group to cancel.</param>
        void CancelEventGroup(uint group)
        {
            if (group == 0 || group > 8 || Empty())
                return;

            foreach (var pair in _eventMap.KeyValueList)
            {
                if (Convert.ToBoolean(pair.Value & (uint)(1 << ((int)group + 15))))
                    _eventMap.Remove(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Returns closest occurence of specified event.
        /// </summary>
        /// <param name="eventId">Wanted event id.</param>
        /// <returns>Time of found event.</returns>
        uint GetNextEventTime(uint eventId)
        {
            if (Empty())
                return 0;

            foreach (var pair in _eventMap.KeyValueList)
                if (eventId == (pair.Value & 0x0000FFFF))
                    return pair.Key;

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Time of next event.</returns>
        uint GetNextEventTime()
        {
            return Empty() ? 0 : _eventMap[0][0];
        }

        /// <summary>
        /// Returns time in milliseconds until next event.
        /// </summary>
        /// <param name="eventId">Id of the event.</param>
        /// <returns>Time of next event.</returns>
        public uint GetTimeUntilEvent(uint eventId)
        {
            foreach (var pair in _eventMap)
                if (eventId == (pair.Value & 0x0000FFFF))
                    return pair.Key - _time;

            return uint.MaxValue;
        }

        /// <summary>
        /// Returns wether event map is in specified phase or not.
        /// </summary>
        /// <param name="phase">Wanted phase.</param>
        /// <returns>True, if phase of event map contains specified phase.</returns>
        public bool IsInPhase(byte phase)
        {
            return phase <= 8 && (phase == 0 || Convert.ToBoolean(_phase & (1 << (phase - 1))));
        }

        /// <summary>
        /// Internal timer.
        /// This does not represent the real date/time value.
        /// It's more like a stopwatch: It can run, it can be stopped,
        /// it can be resetted and so on. Events occur when this timer
        /// has reached their time value. Its value is changed in the Update method.
        /// </summary>
        uint _time;

        /// <summary>
        /// Phase mask of the event map.
        /// Contains the phases the event map is in. Multiple
        /// phases from 1 to 8 can be set with SetPhase or
        /// AddPhase. RemovePhase deactives a phase.
        /// </summary>
        byte _phase;

        /// <summary>
        /// Stores information on the most recently executed event
        /// </summary>
        uint _lastEvent;

        /// <summary>
        /// Key: Time as uint when the event should occur.
        /// Value: The event data as uint.
        /// 
        /// Structure of event data:
        /// - Bit  0 - 15: Event Id.
        /// - Bit 16 - 23: Group
        /// - Bit 24 - 31: Phase
        /// - Pattern: 0xPPGGEEEE
        /// </summary>
        SortedMultiMap<uint, uint> _eventMap = new SortedMultiMap<uint, uint>();
    }
}
