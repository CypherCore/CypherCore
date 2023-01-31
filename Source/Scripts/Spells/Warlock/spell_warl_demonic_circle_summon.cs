// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using Game.Spells.Auras.EffectHandlers;

namespace Scripts.Spells.Warlock
{
    [SpellScript(48018)] // 48018 - Demonic Circle: Summon
    internal class spell_warl_demonic_circle_summon : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects { get; } = new();

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
            Effects.Add(new EffectPeriodicHandler(HandleDummyTick, 0, AuraType.PeriodicDummy));
        }

        private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            // If effect is Removed by expire Remove the summoned demonic circle too.
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Reapply))
                GetTarget().RemoveGameObject(GetId(), true);

            GetTarget().RemoveAura(SpellIds.DEMONIC_CIRCLE_ALLOW_CAST);
        }

        private void HandleDummyTick(AuraEffect aurEff)
        {
            GameObject circle = GetTarget().GetGameObject(GetId());

            if (circle)
            {
                // Here we check if player is in demonic circle teleport range, if so add
                // WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST; allowing him to cast the WARLOCK_DEMONIC_CIRCLE_TELEPORT.
                // If not in range Remove the WARLOCK_DEMONIC_CIRCLE_ALLOW_CAST.

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.DEMONIC_CIRCLE_TELEPORT, GetCastDifficulty());

                if (GetTarget().IsWithinDist(circle, spellInfo.GetMaxRange(true)))
                {
                    if (!GetTarget().HasAura(SpellIds.DEMONIC_CIRCLE_ALLOW_CAST))
                        GetTarget().CastSpell(GetTarget(), SpellIds.DEMONIC_CIRCLE_ALLOW_CAST, true);
                }
                else
                {
                    GetTarget().RemoveAura(SpellIds.DEMONIC_CIRCLE_ALLOW_CAST);
                }
            }
        }
    }
}