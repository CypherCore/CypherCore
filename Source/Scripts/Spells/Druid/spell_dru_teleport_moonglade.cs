// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid
{
    // Teleport : Moonglade - 18960
    [SpellScript(18960)]
    public class spell_dru_teleport_moonglade : SpellScript, ISpellAfterCast
    {


        public void AfterCast()
        {
            Player _player = GetCaster().ToPlayer();
            if (_player != null)
            {
                _player.TeleportTo(1, 7964.063f, -2491.099f, 487.83f, _player.GetOrientation());
            }
        }
    }
}