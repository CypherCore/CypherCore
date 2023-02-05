// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(86213)] // 86213 - Soul Swap Exhale
    internal class spell_warl_soul_swap_exhale : SpellScript, ISpellCheckCast, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(WarlockSpells.SOUL_SWAP_MOD_COST, WarlockSpells.SOUL_SWAP_OVERRIDE);
        }

        public SpellCastResult CheckCast()
        {
            Unit currentTarget = GetExplTargetUnit();
            Unit swapTarget = null;
            Aura swapOverride = GetCaster().GetAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

            if (swapOverride != null)
            {
                spell_warl_soul_swap_override swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>();

                if (swapScript != null)
                    swapTarget = swapScript.GetOriginalSwapSource();
            }

            // Soul Swap Exhale can't be cast on the same Target than Soul Swap
            if (swapTarget &&
                currentTarget &&
                swapTarget == currentTarget)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(onEffectHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void onEffectHit(uint effIndex)
        {
            GetCaster().CastSpell(GetCaster(), WarlockSpells.SOUL_SWAP_MOD_COST, true);
            bool hasGlyph = GetCaster().HasAura(WarlockSpells.GLYPH_OF_SOUL_SWAP);

            List<uint> dotList = new();
            Unit swapSource = null;
            Aura swapOverride = GetCaster().GetAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

            if (swapOverride != null)
            {
                spell_warl_soul_swap_override swapScript = swapOverride.GetScript<spell_warl_soul_swap_override>();

                if (swapScript == null)
                    return;

                dotList = swapScript.GetDotList();
                swapSource = swapScript.GetOriginalSwapSource();
            }

            if (dotList.Empty())
                return;

            foreach (var itr in dotList)
            {
                GetCaster().AddAura(itr, GetHitUnit());

                if (!hasGlyph && swapSource)
                    swapSource.RemoveAurasDueToSpell(itr);
            }

            // Remove Soul Swap Exhale buff
            GetCaster().RemoveAurasDueToSpell(WarlockSpells.SOUL_SWAP_OVERRIDE);

            if (hasGlyph) // Add a cooldown on Soul Swap if caster has the glyph
                GetCaster().CastSpell(GetCaster(), WarlockSpells.SOUL_SWAP_CD_MARKER, false);
        }
    }
}