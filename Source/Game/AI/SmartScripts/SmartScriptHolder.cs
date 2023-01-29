// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.AI
{
    public class SmartScriptHolder : IComparable<SmartScriptHolder>
    {
        public const uint DEFAULT_PRIORITY = uint.MaxValue;
        public SmartAction Action;
        public bool Active { get; set; }
        public bool EnableTimed { get; set; }

        public int EntryOrGuid { get; set; }
        public SmartEvent Event;
        public uint EventId { get; set; }
        public uint Link { get; set; }
        public uint Priority { get; set; }
        public bool RunOnce { get; set; }
        public SmartScriptType SourceType { get; set; }
        public SmartTarget Target;
        public uint Timer { get; set; }

        public SmartScriptHolder()
        {
        }

        public SmartScriptHolder(SmartScriptHolder other)
        {
            EntryOrGuid = other.EntryOrGuid;
            SourceType = other.SourceType;
            EventId = other.EventId;
            Link = other.Link;
            Event = other.Event;
            Action = other.Action;
            Target = other.Target;
            Timer = other.Timer;
            Active = other.Active;
            RunOnce = other.RunOnce;
            EnableTimed = other.EnableTimed;
        }

        public int CompareTo(SmartScriptHolder other)
        {
            int result = Priority.CompareTo(other.Priority);

            if (result == 0)
                result = EntryOrGuid.CompareTo(other.EntryOrGuid);

            if (result == 0)
                result = SourceType.CompareTo(other.SourceType);

            if (result == 0)
                result = EventId.CompareTo(other.EventId);

            if (result == 0)
                result = Link.CompareTo(other.Link);

            return result;
        }

        public SmartScriptType GetScriptType()
        {
            return SourceType;
        }

        public SmartEvents GetEventType()
        {
            return Event.type;
        }

        public SmartActions GetActionType()
        {
            return Action.type;
        }

        public SmartTargets GetTargetType()
        {
            return Target.type;
        }

        public override string ToString()
        {
            return $"Entry {EntryOrGuid} SourceType {GetScriptType()} Event {EventId} Action {GetActionType()}";
        }
    }
}