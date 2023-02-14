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
    // 114852 - Holy Prism (Damage)
    [SpellScript(new uint[] { 114852, 114871 })] // 114871 - Holy Prism (Heal)
    internal class spell_pal_holy_prism_selector : SpellScript, IHasSpellEffects
    {
        private List<WorldObject> _sharedTargets = new();
        private ObjectGuid _targetGUID;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.HolyPrismTargetAlly, PaladinSpells.HolyPrismTargetBeamVisual);
        }

        public override void Register()
        {
            if (ScriptSpellId == PaladinSpells.HolyPrismTargetEnemy)
                SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
            else if (ScriptSpellId == PaladinSpells.HolyPrismTargetAlly)
                SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));

            SpellEffects.Add(new ObjectAreaTargetSelectHandler(ShareTargets, 2, Targets.UnitDestAreaEntry));

            SpellEffects.Add(new EffectHandler(SaveTargetGuid, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new EffectHandler(HandleScript, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        }

        private void SaveTargetGuid(uint effIndex)
        {
            _targetGUID = GetHitUnit().GetGUID();
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            byte maxTargets = 5;

            if (targets.Count > maxTargets)
            {
                if (GetSpellInfo().Id == PaladinSpells.HolyPrismTargetAlly)
                {
                    targets.Sort(new HealthPctOrderPred());
                    targets.Resize(maxTargets);
                }
                else
                {
                    targets.RandomResize(maxTargets);
                }
            }

            _sharedTargets = targets;
        }

        private void ShareTargets(List<WorldObject> targets)
        {
            targets.Clear();
            targets.AddRange(_sharedTargets);
        }

        private void HandleScript(uint effIndex)
        {
            Unit initialTarget = Global.ObjAccessor.GetUnit(GetCaster(), _targetGUID);

            initialTarget?.CastSpell(GetHitUnit(), PaladinSpells.HolyPrismTargetBeamVisual, true);
        }
    }
}
