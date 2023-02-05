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
    // Thal'kiel's Consumption - 211714
    [SpellScript(211714)]
    public class spell_arti_warl_thalkiels_consumption : SpellScript, IHasSpellEffects
    {
        private int _damage = 0;

        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();

        public void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (target == null || caster == null)
            {
                return;
            }

            caster.CastSpell(target, WarlockSpells.THALKIELS_CONSUMPTION_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, _damage));
        }

        public void SaveDamage(List<WorldObject> targets)
        {
            targets.RemoveIf((WorldObject target) =>
            {
                if (!target.IsCreature())
                {
                    return true;
                }
                if (!target.ToCreature().IsPet() || target.ToCreature().ToPet().GetOwner() != GetCaster())
                {
                    return true;
                }
                if (target.ToCreature().GetCreatureType() != CreatureType.Demon)
                {
                    return true;
                }
                return false;
            });

            int basePoints = GetSpellInfo().GetEffect(1).BasePoints;
            foreach (WorldObject pet in targets)
            {
                _damage += (int)pet.ToUnit().CountPctFromMaxHealth(basePoints);
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0,  SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(SaveDamage, 1, Targets.UnitCasterAndSummons));
        }
    }
}