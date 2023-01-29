// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Combat
{
    public class PvPCombatReference : CombatReference
    {
        public static uint PVP_COMBAT_TIMEOUT = 5 * Time.InMilliseconds;

        private uint _combatTimer = PVP_COMBAT_TIMEOUT;


        public PvPCombatReference(Unit first, Unit second) : base(first, second, true)
        {
        }

        public bool Update(uint tdiff)
        {
            if (_combatTimer <= tdiff)
                return false;

            _combatTimer -= tdiff;

            return true;
        }

        public void RefreshTimer()
        {
            _combatTimer = PVP_COMBAT_TIMEOUT;
        }
    }
}