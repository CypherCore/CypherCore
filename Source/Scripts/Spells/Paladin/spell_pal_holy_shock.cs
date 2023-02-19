// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
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
    [SpellScript(20473)] // 20473 - Holy Shock
    internal class spell_pal_holy_shock : SpellScript, ISpellCheckCast, IHasSpellEffects
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.HolyShock, PaladinSpells.HolyShockHealing, PaladinSpells.HolyShockDamage);
        }

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            Unit target = GetExplTargetUnit();

            if (target)
            {
                if (!caster.IsFriendlyTo(target))
                {
                    if (!caster.IsValidAttackTarget(target))
                        return SpellCastResult.BadTargets;

                    if (!caster.IsInFront(target))
                        return SpellCastResult.NotInfront;
                }
            }
            else
            {
                return SpellCastResult.BadTargets;
            }

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDummy(int effIndex)
        {
            Unit caster = GetCaster();
            Unit unitTarget = GetHitUnit();

            if (unitTarget != null)
            {
                if (caster.IsFriendlyTo(unitTarget))
                    caster.CastSpell(unitTarget, PaladinSpells.HolyShockHealing, new CastSpellExtraArgs(GetSpell()));
                else
                    caster.CastSpell(unitTarget, PaladinSpells.HolyShockDamage, new CastSpellExtraArgs(GetSpell()));
            }
        }
    }
}
