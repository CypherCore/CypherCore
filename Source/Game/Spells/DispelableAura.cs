// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Spells
{
    public class DispelableAura
    {
        private readonly Aura _aura;
        private readonly int _chance;
        private byte _charges;

        public DispelableAura(Aura aura, int dispelChance, byte dispelCharges)
        {
            _aura = aura;
            _chance = dispelChance;
            _charges = dispelCharges;
        }

        public bool RollDispel()
        {
            return RandomHelper.randChance(_chance);
        }

        public Aura GetAura()
        {
            return _aura;
        }

        public byte GetDispelCharges()
        {
            return _charges;
        }

        public void IncrementCharges()
        {
            ++_charges;
        }

        public bool DecrementCharge(byte charges)
        {
            if (_charges == 0)
                return false;

            _charges -= charges;

            return _charges > 0;
        }
    }
}