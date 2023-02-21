// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [SpellScript(5740)] // 5740 - Rain of Fire Updated 7.1.5
    internal class spell_warl_rain_of_fire_SpellScript : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            GetCaster()?.RemoveAura(WarlockSpells.RITUAL_OF_RUIN_FREE_CAST_AURA);
        }
    }
}