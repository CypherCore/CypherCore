// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Paladin
{
    // 185984 - Light of Dawn aoe heal
    [SpellScript(185984)]
    public class spell_pal_light_of_dawn_trigger : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        private void FilterTargets(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            byte limit = 5;

            targets.RemoveIf((WorldObject target) =>
            {
                Position pos = target.GetPosition();
                return !(caster.IsWithinDist2d(pos, 15.0f) && caster.IsInFront(target, (float)(Math.PI / 3)));
            });

            targets.RandomResize(limit);
        }

        private void HandleOnHit(int effIndex)
        {
            double dmg = GetHitHeal();
            dmg += GetCaster().m_unitData.AttackPower * 1.8f;

            SetHitHeal(dmg);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
        }
    }
}
