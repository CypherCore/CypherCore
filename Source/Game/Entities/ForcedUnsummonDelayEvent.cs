// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Dynamic;

namespace Game.Entities
{
    public class ForcedUnsummonDelayEvent : BasicEvent
    {
        private readonly TempSummon _owner;

        public ForcedUnsummonDelayEvent(TempSummon owner)
        {
            _owner = owner;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            _owner.UnSummon();

            return true;
        }
    }
}