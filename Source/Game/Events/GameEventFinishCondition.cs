// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class GameEventFinishCondition
    {
        public float Done { get; set; }            // done number
        public uint Done_world_state { get; set; } // done resource Count world State update Id
        public uint Max_world_state { get; set; }  // max resource Count world State update Id
        public float ReqNum { get; set; }          // required number // use float, since some events use percent
    }
}