// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(92795)] //! Soul Swap Copy Spells - 92795 - Simply copies spell IDs.
    internal class spell_warl_soul_swap_dot_marker : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHit(uint effIndex)
        {
            Unit swapVictim = GetCaster();
            Unit warlock = GetHitUnit();

            if (!warlock ||
                !swapVictim)
                return;

            var appliedAuras = swapVictim.GetAppliedAuras();
            spell_warl_soul_swap_override swapSpellScript = null;
            Aura swapOverrideAura = warlock.GetAura(SpellIds.SOUL_SWAP_OVERRIDE);

            if (swapOverrideAura != null)
                swapSpellScript = swapOverrideAura.GetScript<spell_warl_soul_swap_override>();

            if (swapSpellScript == null)
                return;

            FlagArray128 classMask = GetEffectInfo().SpellClassMask;

            foreach (var itr in appliedAuras.KeyValueList)
            {
                SpellInfo spellProto = itr.Value.GetBase().GetSpellInfo();

                if (itr.Value.GetBase().GetCaster() == warlock)
                    if (spellProto.SpellFamilyName == SpellFamilyNames.Warlock &&
                        (spellProto.SpellFamilyFlags & classMask))
                        swapSpellScript.AddDot(itr.Key);
            }

            swapSpellScript.SetOriginalSwapSource(swapVictim);
        }
    }
}