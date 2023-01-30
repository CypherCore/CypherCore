// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Conditions;

namespace Game
{
    public class PhaseRef
    {
        public List<Condition> AreaConditions { get; set; }

        public PhaseFlags Flags { get; set; }
        public int References { get; set; }

        public PhaseRef(PhaseFlags flags, List<Condition> conditions)
        {
            Flags = flags;
            References = 0;
            AreaConditions = conditions;
        }

        public bool IsPersonal()
        {
            return Flags.HasFlag(PhaseFlags.Personal);
        }
    }
}