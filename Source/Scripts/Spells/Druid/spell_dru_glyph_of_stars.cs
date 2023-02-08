// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 24858 - Moonkin Form
    internal class spell_dru_glyph_of_stars : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(DruidSpellIds.GlyphOfStars, DruidSpellIds.GlyphOfStarsVisual);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
            AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }

        private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();

            if (target.HasAura(DruidSpellIds.GlyphOfStars))
                target.CastSpell(target, DruidSpellIds.GlyphOfStarsVisual, true);
        }

        private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            GetTarget().RemoveAurasDueToSpell(DruidSpellIds.GlyphOfStarsVisual);
        }
    }
}