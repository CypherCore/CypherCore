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
    public class TaskScheduler
    {
        public TaskScheduler()
        {
            _now = DateTime.Now;
            _task_holder = new TaskQueue();
            _asyncHolder = new List<Action>();
            _predicate = EmptyValidator;
        }

        public TaskScheduler(predicate_t predicate)
        {
            _now = DateTime.Now;
            _task_holder = new TaskQueue();
            _asyncHolder = new List<Action>();
            _predicate = predicate;
        }

        /// <summary>
        /// Clears the validator which is asked if tasks are allowed to be executed.
        /// </summary>
        /// <returns></returns>
        TaskScheduler ClearValidator()
        {
            _predicate = EmptyValidator;
            return this;
        }

        /// <summary>
        /// Sets a validator which is asked if tasks are allowed to be executed.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public TaskScheduler SetValidator(predicate_t predicate)
        {
            _predicate = predicate;
            return this;
        }

        /// <summary>
        /// Update the scheduler to the current time.
        /// Calls the optional callback on successfully finish.
        /// </summary>
        /// <returns></returns>
        public TaskScheduler Update(success_t callback = null)
        {
            _now = DateTime.Now;
            Dispatch(callback);
            return this;
        }

        /// <summary>
        /// Update the scheduler with a difftime in ms.
        /// Calls the optional callback on successfully finish.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public TaskScheduler Update(uint milliseconds, success_t callback = null)
        {
            return Update(TimeSpan.FromMilliseconds(milliseconds), callback);
        }

        /// <summary>
        /// Update the scheduler with a difftime.
        /// Calls the optional callback on successfully finish.
        /// </summary>
        /// <param name="difftime"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        TaskScheduler Update(TimeSpan difftime, success_t callback = null)
        {
            _now += difftime;
            Dispatch(callback);
            return this;
        }

        public TaskScheduler Async(Action callable)
        {
            _asyncHolder.Add(callable);
            return this;
        }

        /// <summary>
        /// Schedule an event with a fixed rate.
        /// Never call this from within a task context! Use TaskContext.Schedule instead!
        /// </summary>
        /// <param name="time"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskScheduler Schedule(TimeSpan time, Action<TaskContext> task)
        {
            return ScheduleAt(_now, time, task);
        }

        /// <summary>
        /// Schedule an event with a fixed rate.
        /// Never call this from within a task context! Use TaskContext.Schedule instead!
        /// </summary>
        /// <param name="time"></param>
        /// <param name="group"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskScheduler Schedule(TimeSpan time, uint group, Action<TaskContext> task)
        {
            return ScheduleAt(_now, time, group, task);
        }

        /// <summary>
        /// Schedule an event with a randomized rate between min and max rate.
        /// Never call this from within a task context! Use TaskContext.Schedule instead!
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskScheduler Schedule(TimeSpan min, TimeSpan max, Action<TaskContext> task)
        {
            return Schedule(RandomDurationBetween(min, max), task);
        }

        /// <summary>
        /// Schedule an event with a fixed rate.
        /// Never call this from within a task context! Use TaskContext.Schedule instead!
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="group"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskScheduler Schedule(TimeSpan min, TimeSpan max, uint group, Action<TaskContext> task)
        {
            return Schedule(RandomDurationBetween(min, max), group, task);
        }

        public TaskScheduler CancelAll()
        {
            // Clear the task holder
            _task_holder.Clear();
            _asyncHolder.Clear();
            return this;
        }

        public TaskScheduler CancelGroup(uint group)
        {
            _task_holder.RemoveIf(task => task.IsInGroup(@group));
            return this;
        }

        public TaskScheduler CancelGroupsOf(List<uint> groups)
        {
            groups.ForEach(group => CancelGroup(group));

            return this;
        }

        /// <summary>
        /// Delays all tasks with the given duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskScheduler DelayAll(TimeSpan duration)
        {
            _task_holder.ModifyIf(task =>
            {
                task._end += duration;
                return true;
            });
            return this;
        }

        /// <summary>
        /// Delays all tasks with a random duration between min and max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskScheduler DelayAll(TimeSpan min, TimeSpan max)
        {
            return DelayAll(RandomDurationBetween(min, max));
        }

        /// <summary>
        /// Delays all tasks of a group with the given duration.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskScheduler DelayGroup(uint group, TimeSpan duration)
        {
            _task_holder.ModifyIf(task =>
            {
                if (task.IsInGroup(group))
                {
                    task._end += duration;
                    return true;
                }
                else
                    return false;
            });
            return this;
        }

        /// <summary>
        /// Delays all tasks of a group with a random duration between min and max.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskScheduler DelayGroup(uint group, TimeSpan min, TimeSpan max)
        {
            return DelayGroup(group, RandomDurationBetween(min, max));
        }

        /// <summary>
        /// Reschedule all tasks with a given duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskScheduler RescheduleAll(TimeSpan duration)
        {
            var end = _now + duration;
            _task_holder.ModifyIf(task =>
            {
                task._end = end;
                return true;
            });
            return this;
        }

        /// <summary>
        /// Reschedule all tasks with a random duration between min and max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskScheduler RescheduleAll(TimeSpan min, TimeSpan max)
        {
            return RescheduleAll(RandomDurationBetween(min, max));
        }

        /// <summary>
        /// Reschedule all tasks of a group with the given duration.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskScheduler RescheduleGroup(uint group, TimeSpan duration)
        {
            var end = _now + duration;
            _task_holder.ModifyIf(task =>
            {
                if (task.IsInGroup(group))
                {
                    task._end = end;
                    return true;
                }
                else
                    return false;
            });
            return this;
        }

        /// <summary>
        /// Reschedule all tasks of a group with a random duration between min and max.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskScheduler RescheduleGroup(uint group, TimeSpan min, TimeSpan max)
        {
            return RescheduleGroup(group, RandomDurationBetween(min, max));
        }

        internal TaskScheduler InsertTask(Task task)
        {
            _task_holder.Push(task);
            return this;
        }

        internal TaskScheduler ScheduleAt(DateTime end, TimeSpan time, Action<TaskContext> task)
        {
            return InsertTask(new Task(end + time, time, task));
        }

        /// <summary>
        /// Schedule an event with a fixed rate.
        /// Never call this from within a task context! Use TaskContext.schedule instead!
        /// </summary>
        /// <param name="end"></param>
        /// <param name="time"></param>
        /// <param name="group"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        internal TaskScheduler ScheduleAt(DateTime end, TimeSpan time, uint group, Action<TaskContext> task)
        {
            return InsertTask(new Task(end + time, time, group, 0, task));
        }

        /// <summary>
        /// Returns a random duration between min and max
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>TimeSpan</returns>
        public static TimeSpan RandomDurationBetween(TimeSpan min, TimeSpan max)
        {
            var milli_min = min.TotalMilliseconds;
            var milli_max = max.TotalMilliseconds;

            // TC specific: use SFMT URandom
            return TimeSpan.FromMilliseconds(RandomHelper.URand(milli_min, milli_max));
        }

        void Dispatch(success_t callback = null)
        {
            // If the validation failed abort the dispatching here.
            if (!_predicate())
                return;

            // Process all asyncs
            while (!_asyncHolder.Empty())
            {
                _asyncHolder.First().Invoke();
                _asyncHolder.RemoveAt(0);

                // If the validation failed abort the dispatching here.
                if (!_predicate())
                    return;
            }

            while (!_task_holder.IsEmpty())
            {
                if (_task_holder.First()._end > _now)
                    break;

                // Perfect forward the context to the handler
                // Use weak references to catch destruction before callbacks.
                TaskContext context = new TaskContext(_task_holder.Pop(), this);

                // Invoke the context
                context.Invoke();

                // If the validation failed abort the dispatching here.
                if (!_predicate())
                    return;
            }

            callback?.Invoke();
        }

        // The current time point (now)
        DateTime _now;

        // The Task Queue which contains all task objects.
        TaskQueue _task_holder;

        // Contains all asynchronous tasks which will be invoked at
        // the next update tick.
        List<Action> _asyncHolder;

        predicate_t _predicate;

        static bool EmptyValidator()
        {
            return true;
        }

        // Predicate type
        public delegate bool predicate_t();
        // Success handle type
        public delegate void success_t();
    }

    public class Task : IComparable<Task>
    {
        public Task(DateTime end, TimeSpan duration, uint group, uint repeated, Action<TaskContext> task)
        {
            _end = end;
            _duration = duration;
            _group.Set(group);
            _repeated = repeated;
            _task = task;
        }

        public Task(DateTime end, TimeSpan duration, Action<TaskContext> task)
        {
            _end = end;
            _duration = duration;
            _task = task;
        }

        public int CompareTo(Task other)
        {
            return _end.CompareTo(other._end);
        }

        /// <summary>
        /// Returns true if the task is in the given group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool IsInGroup(uint group)
        {
            return _group.HasValue && _group.Value == group;
        }

        internal DateTime _end;
        internal TimeSpan _duration;
        internal Optional<uint> _group;
        internal uint _repeated;
        internal Action<TaskContext> _task;
    }

    class TaskQueue
    {
        /// <summary>
        /// Pushes the task in the container
        /// </summary>
        /// <param name="task"></param>
        public void Push(Task task)
        {
            if (!container.Add(task))
            {

            }
        }

        /// <summary>
        /// Pops the task out of the container
        /// </summary>
        /// <returns></returns>
        public Task Pop()
        {
            Task result = container.First();
            container.Remove(result);
            return result;
        }

        public Task First()
        {
            return container.First();
        }

        public void Clear()
        {
            container.Clear();
        }

        public void RemoveIf(Predicate<Task> filter)
        {
            container.RemoveWhere(filter);
        }

        public void ModifyIf(Func<Task, bool> filter)
        {
            List<Task> cache = new List<Task>();
            foreach (var task in container.Where(filter))
            {
                if (filter(task))
                {
                    cache.Add(task);
                    container.Remove(task);
                }
            }

            foreach (var task in cache)
                container.Add(task);
        }

        public bool IsEmpty()
        {
            return container.Empty();
        }

        SortedSet<Task> container = new SortedSet<Task>();
    }

    public class TaskContext
    {
        public TaskContext(Task task, TaskScheduler owner)
        {
            _task = task;
            _owner = owner;
            _consumed = false;
        }

        /// <summary>
        /// Dispatches an action safe on the TaskScheduler
        /// </summary>
        /// <param name="apply"></param>
        /// <returns></returns>
        TaskContext Dispatch(Action apply)
        {
            apply();

            return this;
        }

        TaskContext Dispatch(Func<TaskScheduler, TaskScheduler> apply)
        {
            apply(_owner);

            return this;
        }

        bool IsExpired()
        {
            return _owner == null;
        }
        
        /// <summary>
        /// Returns true if the event is in the given group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        bool IsInGroup(uint group)
        {
            return _task.IsInGroup(group);
        }

        /// <summary>
        /// Sets the event in the given group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        TaskContext SetGroup(uint group)
        {
            _task._group.Set(group);
            return this;
        }

        /// <summary>
        /// Removes the group from the event
        /// </summary>
        /// <returns></returns>
        TaskContext ClearGroup()
        {
            _task._group.HasValue = false;
            return this;
        }

        /// <summary>
        /// Returns the repeat counter which increases every time the task is repeated.
        /// </summary>
        /// <returns></returns>
        uint GetRepeatCounter()
        {
            return _task._repeated;
        }

        /// <summary>
        /// Schedule a callable function that is executed at the next update tick from within the context.
        /// Its safe to modify the TaskScheduler from within the callable.
        /// </summary>
        /// <param name="callable"></param>
        /// <returns></returns>
        TaskContext Async(Action callable)
        {
            return Dispatch(() => _owner.Async(callable));
        }

        /// <summary>
        /// Cancels all tasks from within the context.
        /// </summary>
        /// <returns></returns>
        public TaskContext CancelAll()
        {
            return Dispatch(() => _owner.CancelAll());
        }

        /// <summary>
        /// Cancel all tasks of a single group from within the context.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public TaskContext CancelGroup(uint group)
        {
            return Dispatch(() => _owner.CancelGroup(group));
        }

        /// <summary>
        /// Cancels all groups in the given std.vector from within the context.
        /// </summary>
        /// <param name="groups"></param>
        /// <returns></returns>
        public TaskContext CancelGroupsOf(List<uint> groups)
        {
            return Dispatch(() => _owner.CancelGroupsOf(groups));
        }

        /// <summary>
        /// Asserts if the task was consumed already.
        /// </summary>
        void AssertOnConsumed()
        {
            // This was adapted to TC to prevent static analysis tools from complaining.
            // If you encounter this assertion check if you repeat a TaskContext more then 1 time!
            Cypher.Assert(!_consumed, "Bad task logic, task context was consumed already!");
        }

        /// <summary>
        /// Invokes the associated hook of the task.
        /// </summary>
        public void Invoke()
        {
            _task._task(this);
        }

        /// <summary>
        /// Repeats the event and sets a new duration.
        /// This will consume the task context, its not possible to repeat the task again
        /// from the same task context!
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskContext Repeat(TimeSpan duration)
        {
            AssertOnConsumed();

            // Set new duration, in-context timing and increment repeat counter
            _task._duration = duration;
            _task._end += duration;
            _task._repeated += 1;
            _consumed = true;
            return Dispatch(() => _owner.InsertTask(_task));
        }

        /// <summary>
        /// Repeats the event with the same duration.
        /// This will consume the task context, its not possible to repeat the task again
        /// from the same task context!
        /// </summary>
        /// <returns></returns>
        public TaskContext Repeat()
        {
            return Repeat(_task._duration);
        }

        /// <summary>
        /// Repeats the event and set a new duration that is randomized between min and max.
        /// This will consume the task context, its not possible to repeat the task again
        /// from the same task context!
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskContext Repeat(TimeSpan min, TimeSpan max)
        {
            return Repeat(TaskScheduler.RandomDurationBetween(min, max));
        }

        /// <summary>
        /// Schedule an event with a fixed rate from within the context.
        /// Its possible that the new event is executed immediately!
        /// Use TaskScheduler.Async to create a task
        /// which will be called at the next update tick.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskContext Schedule(TimeSpan time, Action<TaskContext> task)
        {
            var end = _task._end;
            return Dispatch(scheduler => scheduler.ScheduleAt(end, time, task));
        }
        public TaskContext Schedule(TimeSpan time, Action task) { return Schedule(time, delegate (TaskContext task1) { task(); }); }

        /// <summary>
        /// Schedule an event with a fixed rate from within the context.
        /// Its possible that the new event is executed immediately!
        /// Use TaskScheduler.Async to create a task
        /// which will be called at the next update tick.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="group"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskContext Schedule(TimeSpan time, uint group, Action<TaskContext> task)
        {
            var end = _task._end;
            return Dispatch(scheduler => scheduler.ScheduleAt(end, time, @group, task));
        }
        public TaskContext Schedule(TimeSpan time, uint group, Action task) { return Schedule(time, group, delegate (TaskContext task1) { task(); }); }

        /// <summary>
        /// Schedule an event with a randomized rate between min and max rate from within the context.
        /// Its possible that the new event is executed immediately!
        /// Use TaskScheduler.Async to create a task
        /// which will be called at the next update tick.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskContext Schedule(TimeSpan min, TimeSpan max, Action<TaskContext> task)
        {
            return Schedule(TaskScheduler.RandomDurationBetween(min, max), task);
        }
        public TaskContext Schedule(TimeSpan min, TimeSpan max, Action task) { return Schedule(min, max, delegate (TaskContext task1) { task(); }); }

        /// <summary>
        /// Schedule an event with a randomized rate between min and max rate from within the context.
        /// Its possible that the new event is executed immediately!
        /// Use TaskScheduler.Async to create a task
        /// which will be called at the next update tick.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="group"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public TaskContext Schedule(TimeSpan min, TimeSpan max, uint group, Action<TaskContext> task)
        {
            return Schedule(TaskScheduler.RandomDurationBetween(min, max), group, task);
        }
        public TaskContext Schedule(TimeSpan min, TimeSpan max, uint group, Action task) { return Schedule(min, max, group, delegate (TaskContext task1) { task(); }); }

        /// <summary>
        /// Delays all tasks with the given duration from within the context.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskContext DelayAll(TimeSpan duration)
        {
            return Dispatch(() => _owner.DelayAll(duration));
        }

        /// <summary>
        /// Delays all tasks with a random duration between min and max from within the context.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskContext DelayAll(TimeSpan min, TimeSpan max)
        {
            return DelayAll(TaskScheduler.RandomDurationBetween(min, max));
        }

        /// <summary>
        /// Delays all tasks of a group with the given duration from within the context.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskContext DelayGroup(uint group, TimeSpan duration)
        {
            return Dispatch(() => _owner.DelayGroup(group, duration));
        }

        /// <summary>
        /// Delays all tasks of a group with a random duration between min and max from within the context.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskContext DelayGroup(uint group, TimeSpan min, TimeSpan max)
        {
            return DelayGroup(group, TaskScheduler.RandomDurationBetween(min, max));
        }

        /// <summary>
        /// Reschedule all tasks with the given duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskContext RescheduleAll(TimeSpan duration)
        {
            return Dispatch(() => _owner.RescheduleAll(duration));
        }

        /// <summary>
        /// Reschedule all tasks with a random duration between min and max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskContext RescheduleAll(TimeSpan min, TimeSpan max)
        {
            return RescheduleAll(TaskScheduler.RandomDurationBetween(min, max));
        }

        /// <summary>
        /// Reschedule all tasks of a group with the given duration.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public TaskContext RescheduleGroup(uint group, TimeSpan duration)
        {
            return Dispatch(() => _owner.RescheduleGroup(group, duration));
        }

        /// <summary>
        /// Reschedule all tasks of a group with a random duration between min and max.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public TaskContext RescheduleGroup(uint group, TimeSpan min, TimeSpan max)
        {
            return RescheduleGroup(group, TaskScheduler.RandomDurationBetween(min, max));
        }

        // Associated task
        Task _task;

        // Owner
        TaskScheduler _owner;

        // Marks the task as consumed
        bool _consumed = true;
    }
}
