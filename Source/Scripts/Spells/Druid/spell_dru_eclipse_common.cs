// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    internal class spell_dru_eclipse_common
    {
        public static void SetSpellCount(Unit unitOwner, uint spellId, uint amount)
        {
            Aura aura = unitOwner.GetAura(spellId);

            if (aura == null)
                unitOwner.CastSpell(unitOwner, spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)amount));
            else
                aura.SetStackAmount((byte)amount);
        }
    }
}