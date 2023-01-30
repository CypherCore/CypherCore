// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Dynamic;

namespace Game.Spells
{
    public class ChargeDropEvent : BasicEvent
    {
        private readonly Aura _base;
        private readonly AuraRemoveMode _mode;

        public ChargeDropEvent(Aura aura, AuraRemoveMode mode)
        {
            _base = aura;
            _mode = mode;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            // _base is always valid (look in Aura._Remove())
            _base.ModChargesDelayed(-1, _mode);

            return true;
        }
    }
}