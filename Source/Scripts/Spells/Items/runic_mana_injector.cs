// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items
{
    [SpellScript(42545)]
    internal class runic_mana_injector : SpellScript, ISpellEnergizedBySpell
    {
        public void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType)
        {
            Player player = target.ToPlayer();
            if (player != null)
                if (player.HasSkill(SkillType.Engineering))
                    MathFunctions.AddPct(ref amount, 25);
        }
    }
}
