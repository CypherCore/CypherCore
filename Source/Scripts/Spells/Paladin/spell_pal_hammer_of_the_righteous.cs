// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(53595)] // 53595 - Hammer of the Righteous
    internal class spell_pal_hammer_of_the_righteous : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.ConsecrationProtectionAura, PaladinSpells.HammerOfTheRighteousAoe);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleAoEHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleAoEHit(uint effIndex)
        {
            if (GetCaster().HasAura(PaladinSpells.ConsecrationProtectionAura))
                GetCaster().CastSpell(GetHitUnit(), PaladinSpells.HammerOfTheRighteousAoe);
        }
    }
}
