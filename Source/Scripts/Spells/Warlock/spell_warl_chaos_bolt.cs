// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [Script] // 116858 - Chaos Bolt
    internal class spell_warl_chaos_bolt : SpellScript, IHasSpellEffects, ICalcCritChance
    {
        public override bool Load()
        {
            return GetCaster().IsPlayer();
        }

        public void CalcCritChance(Unit victim, ref float critChance)
        {
            critChance = 100.0f;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(uint effIndex)
        {
            SetHitDamage(GetHitDamage() + MathFunctions.CalculatePct(GetHitDamage(), GetCaster().ToPlayer().ActivePlayerData.SpellCritPercentage));
        }
    }
}