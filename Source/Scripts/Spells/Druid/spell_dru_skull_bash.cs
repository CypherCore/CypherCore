// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 106839 - Skull Bash
    internal class spell_dru_skull_bash : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.SkullBashCharge, DruidSpellIds.SkullBashInterrupt);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), DruidSpellIds.SkullBashCharge, true);
            GetCaster().CastSpell(GetHitUnit(), DruidSpellIds.SkullBashInterrupt, true);
        }
    }
}