// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game
{
    public class GameEventData
    {
        public GameEventData()
        {
            Start = 1;
        }

        public byte Announce { get; set; }                                                  // if 0 dont announce, if 1 announce, if 2 take config value
        public Dictionary<uint, GameEventFinishCondition> Conditions { get; set; } = new(); // conditions to finish
        public string Description { get; set; }
        public long End { get; set; } // occurs before this Time
        public HolidayIds Holiday_id { get; set; }
        public byte HolidayStage { get; set; }
        public uint Length { get; set; }                               // length of the event (Time.Minutes) after finishing all conditions
        public long Nextstart { get; set; }                            // after this Time the follow-up events Count this phase completed
        public uint Occurence { get; set; }                            // Time between end and start
        public List<ushort> Prerequisite_events { get; set; } = new(); // events that must be completed before starting this event

        public long Start { get; set; }           // occurs after this Time
        public GameEventState State { get; set; } // State of the game event, these are saved into the game_event table on change!

        public bool IsValid()
        {
            return Length > 0 || State > GameEventState.Normal;
        }
    }
}