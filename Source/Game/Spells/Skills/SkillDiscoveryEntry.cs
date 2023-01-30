// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Spells
{
    public class SkillDiscoveryEntry
    {
        public float Chance { get; set; }       // chance
        public uint ReqSkillValue { get; set; } // skill level limitation

        public uint SpellId { get; set; } // discavered spell

        public SkillDiscoveryEntry(uint _spellId = 0, uint req_skill_val = 0, float _chance = 0)
        {
            SpellId = _spellId;
            ReqSkillValue = req_skill_val;
            Chance = _chance;
        }
    }
}