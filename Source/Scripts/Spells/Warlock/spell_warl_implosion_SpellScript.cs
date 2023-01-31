// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
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
    // 196277 - Implosion
    [SpellScript(SpellIds.IMPLOSION)]
    public class spell_warl_implosion_SpellScript : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();

            if (caster == null ||
                target == null)
                return;

            List<Creature> imps = caster.GetCreatureListWithEntryInGrid(55659); // Wild Imps

            foreach (Creature imp in imps)
                if (imp.ToTempSummon().GetSummoner() == caster)
                {
                    imp.InterruptNonMeleeSpells(false);
                    imp.VariableStorage.Set("controlled", true);
                    imp.VariableStorage.Set("ForceUpdateTimers", true);
                    imp.CastSpell(target, SpellIds.IMPLOSION_JUMP, true);
                    imp.GetMotionMaster().MoveJump(target, 300.0f, 1.0f, EventId.Jump);
                    ObjectGuid casterGuid = caster.GetGUID();

                    imp.GetAI()
                       .Scheduler.Schedule(TimeSpan.FromMilliseconds(500),
                                           task =>
                                           {
                                               imp.CastSpell(imp, SpellIds.IMPLOSION_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(casterGuid));
                                               imp.DisappearAndDie();
                                           });
                }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }

    public class ImplosionDamageEvent : BasicEvent
    {
        private readonly Unit _caster;
        private readonly Unit _target;

        public ImplosionDamageEvent(Unit caster, Unit target)
        {
            _caster = caster;
            _target = target;
        }

        public override bool Execute(ulong UnnamedParameter, uint UnnamedParameter2)
        {
            if (_caster && _target)
            {
                _caster.CastSpell(_target, SpellIds.IMPLOSION_DAMAGE, true);
                _target.ToCreature().DisappearAndDie();
            }

            return true;
        }

    }
}