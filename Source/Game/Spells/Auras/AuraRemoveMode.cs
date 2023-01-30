// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Spells
{
    public enum AuraRemoveMode
    {
        None = 0,
        Default = 1, // scripted remove, remove by stack with aura with different ids and sc aura remove
        Interrupt,
        Cancel,
        EnemySpell, // dispel and Absorb aura destroy
        Expire,     // aura duration has ended
        Death
    }
}