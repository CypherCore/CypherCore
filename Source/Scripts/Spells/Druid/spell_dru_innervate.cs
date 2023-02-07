// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 29166 - Innervate
    internal class spell_dru_innervate : SpellScript, ISpellCheckCast, ISpellOnHit
    {
        public SpellCastResult CheckCast()
        {
            Player target = GetExplTargetUnit()?.ToPlayer();

            if (target == null)
                return SpellCastResult.BadTargets;

            ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(target.GetPrimarySpecialization());

            if (spec == null ||
                spec.Role != 1)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        public void OnHit()
        {
            Unit caster = GetCaster();

            if (caster != GetHitUnit())
            {
                AuraEffect innervateR2 = caster.GetAuraEffect(DruidSpellIds.InnervateRank2, 0);

                if (innervateR2 != null)
                    caster.CastSpell(caster,
                                     DruidSpellIds.Innervate,
                                     new CastSpellExtraArgs(TriggerCastFlags.IgnoreSpellAndCategoryCD | TriggerCastFlags.IgnoreCastInProgress)
                                         .SetTriggeringSpell(GetSpell())
                                         .AddSpellMod(SpellValueMod.BasePoint0, -innervateR2.GetAmount()));
            }
        }
    }
}