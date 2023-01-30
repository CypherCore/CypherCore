// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    public class PetAura
    {
        private readonly Dictionary<uint, uint> _auras = new();
        private readonly int _damage;
        private readonly bool _removeOnChangePet;

        public PetAura()
        {
            _removeOnChangePet = false;
            _damage = 0;
        }

        public PetAura(uint petEntry, uint aura, bool _removeOnChangePet, int _damage)
        {
            this._removeOnChangePet = _removeOnChangePet;
            this._damage = _damage;

            _auras[petEntry] = aura;
        }

        public uint GetAura(uint petEntry)
        {
            var auraId = _auras.LookupByKey(petEntry);

            if (auraId != 0)
                return auraId;

            auraId = _auras.LookupByKey(0);

            if (auraId != 0)
                return auraId;

            return 0;
        }

        public void AddAura(uint petEntry, uint aura)
        {
            _auras[petEntry] = aura;
        }

        public bool IsRemovedOnChangePet()
        {
            return _removeOnChangePet;
        }

        public int GetDamage()
        {
            return _damage;
        }
    }
}